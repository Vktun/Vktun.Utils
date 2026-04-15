using System.Collections;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;

namespace Vktun.Engine.Document;

/// <summary>
/// Default variable renderer for HTML and text templates.
/// </summary>
public sealed class DefaultDocumentTemplateRenderer : IDocumentTemplateRenderer
{
    private static readonly Regex PlaceholderPattern = new(
        @"\{\{\s*(?<name>[A-Za-z_][\w.]*)(?:\s*:\s*(?<format>[^}]+?))?\s*\}\}|\{(?<singleName>[A-Za-z_][\w.]*)(?::(?<singleFormat>[^}]+?))?\}",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <inheritdoc />
    public Task<string> RenderAsync(
        string templateContent,
        DocumentRenderRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(templateContent);
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var values = BuildValueMap(request);
        var culture = request.GetCulture();

        var rendered = PlaceholderPattern.Replace(templateContent, match =>
        {
            var name = GetMatchValue(match, "name", "singleName");
            var format = GetMatchValue(match, "format", "singleFormat");

            if (!TryResolveValue(values, name, out var value))
            {
                return request.MissingVariableBehavior switch
                {
                    DocumentMissingVariableBehavior.KeepPlaceholder => match.Value,
                    DocumentMissingVariableBehavior.Throw => throw new KeyNotFoundException(
                        $"Document template variable '{name}' was not found."),
                    _ => string.Empty
                };
            }

            var text = FormatValue(value, format, culture);
            return request.HtmlEncodeValues ? HtmlEncoder.Default.Encode(text) : text;
        });

        return Task.FromResult(rendered);
    }

    private static string GetMatchValue(Match match, string primary, string secondary)
    {
        var value = match.Groups[primary].Value;
        return string.IsNullOrWhiteSpace(value) ? match.Groups[secondary].Value : value;
    }

    private static Dictionary<string, object?> BuildValueMap(DocumentRenderRequest request)
    {
        var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        if (request.Model is not null)
        {
            AddObjectValues(values, request.Model);
        }

        foreach (var item in request.Values)
        {
            values[item.Key] = item.Value;
        }

        return values;
    }

    private static void AddObjectValues(IDictionary<string, object?> values, object model)
    {
        if (model is IEnumerable<KeyValuePair<string, object?>> typedDictionary)
        {
            foreach (var item in typedDictionary)
            {
                values[item.Key] = item.Value;
            }

            return;
        }

        if (model is IDictionary dictionary)
        {
            foreach (DictionaryEntry item in dictionary)
            {
                if (item.Key is string key)
                {
                    values[key] = item.Value;
                }
            }

            return;
        }

        foreach (var property in model.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (property.GetIndexParameters().Length > 0)
            {
                continue;
            }

            values[property.Name] = property.GetValue(model);
        }
    }

    private static bool TryResolveValue(
        IReadOnlyDictionary<string, object?> values,
        string path,
        out object? value)
    {
        if (values.TryGetValue(path, out value))
        {
            return true;
        }

        var segments = path.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Length == 0 || !values.TryGetValue(segments[0], out value))
        {
            value = null;
            return false;
        }

        for (var index = 1; index < segments.Length; index++)
        {
            if (!TryReadMember(value, segments[index], out value))
            {
                value = null;
                return false;
            }
        }

        return true;
    }

    private static bool TryReadMember(object? source, string memberName, out object? value)
    {
        value = null;
        if (source is null)
        {
            return false;
        }

        if (source is IDictionary<string, object?> typedDictionary)
        {
            return typedDictionary.TryGetValue(memberName, out value);
        }

        if (source is IDictionary dictionary && dictionary.Contains(memberName))
        {
            value = dictionary[memberName];
            return true;
        }

        var property = source
            .GetType()
            .GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);

        if (property is null || property.GetIndexParameters().Length > 0)
        {
            return false;
        }

        value = property.GetValue(source);
        return true;
    }

    private static string FormatValue(object? value, string? format, IFormatProvider formatProvider)
    {
        if (value is null)
        {
            return string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(format) && value is IFormattable formattable)
        {
            return formattable.ToString(format.Trim(), formatProvider) ?? string.Empty;
        }

        return Convert.ToString(value, formatProvider) ?? string.Empty;
    }
}
