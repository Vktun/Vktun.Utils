using System.Globalization;

namespace Vktun.Engine.Document;

/// <summary>
/// Common document content types.
/// </summary>
public static class DocumentContentTypes
{
    /// <summary>
    /// HTML content encoded as UTF-8.
    /// </summary>
    public const string Html = "text/html; charset=utf-8";

    /// <summary>
    /// Plain text content encoded as UTF-8.
    /// </summary>
    public const string PlainText = "text/plain; charset=utf-8";

    /// <summary>
    /// PDF binary content.
    /// </summary>
    public const string Pdf = "application/pdf";

    /// <summary>
    /// Generic binary content.
    /// </summary>
    public const string OctetStream = "application/octet-stream";
}

/// <summary>
/// Defines how template rendering handles a placeholder that cannot be resolved.
/// </summary>
public enum DocumentMissingVariableBehavior
{
    /// <summary>
    /// Replaces the missing placeholder with an empty string.
    /// </summary>
    EmptyString = 0,

    /// <summary>
    /// Keeps the original placeholder text.
    /// </summary>
    KeepPlaceholder = 1,

    /// <summary>
    /// Throws an exception when a placeholder is missing.
    /// </summary>
    Throw = 2
}

/// <summary>
/// Describes a template resolved from a store such as a database or file system.
/// </summary>
public sealed class DocumentTemplateDescriptor
{
    /// <summary>
    /// Gets or sets the template code.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the template content.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the template source content type.
    /// </summary>
    public string ContentType { get; set; } = DocumentContentTypes.Html;
}

/// <summary>
/// Describes one document render request.
/// </summary>
public sealed class DocumentRenderRequest
{
    /// <summary>
    /// Gets or sets the template code used by <see cref="IDocumentTemplateResolver"/>.
    /// </summary>
    public string? TemplateCode { get; set; }

    /// <summary>
    /// Gets or sets inline template content. When this is set, the resolver is not used.
    /// </summary>
    public string? TemplateContent { get; set; }

    /// <summary>
    /// Gets or sets an optional object model. Public properties can be referenced by placeholders.
    /// </summary>
    public object? Model { get; set; }

    /// <summary>
    /// Gets template values. Values override properties with the same name from <see cref="Model"/>.
    /// </summary>
    public IDictionary<string, object?> Values { get; set; } =
        new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or sets the requested output file name.
    /// </summary>
    public string FileName { get; set; } = "document.html";

    /// <summary>
    /// Gets or sets the requested output content type.
    /// </summary>
    public string OutputContentType { get; set; } = DocumentContentTypes.Html;

    /// <summary>
    /// Gets or sets the culture name used for value formatting.
    /// </summary>
    public string? CultureName { get; set; }

    /// <summary>
    /// Gets or sets how unresolved placeholders are handled.
    /// </summary>
    public DocumentMissingVariableBehavior MissingVariableBehavior { get; set; } =
        DocumentMissingVariableBehavior.EmptyString;

    /// <summary>
    /// Gets or sets a value indicating whether rendered placeholder values should be HTML encoded.
    /// </summary>
    public bool HtmlEncodeValues { get; set; }

    /// <summary>
    /// Gets the culture used to format values.
    /// </summary>
    /// <returns>The configured culture or invariant culture.</returns>
    public CultureInfo GetCulture()
    {
        return string.IsNullOrWhiteSpace(CultureName)
            ? CultureInfo.InvariantCulture
            : CultureInfo.GetCultureInfo(CultureName);
    }
}

/// <summary>
/// Represents a rendered document file.
/// </summary>
public sealed class DocumentFileResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentFileResult"/> class.
    /// </summary>
    /// <param name="fileName">The file name.</param>
    /// <param name="contentType">The content type.</param>
    /// <param name="content">The file content.</param>
    public DocumentFileResult(string fileName, string contentType, byte[] content)
    {
        FileName = fileName;
        ContentType = contentType;
        Content = content;
    }

    /// <summary>
    /// Gets the file name.
    /// </summary>
    public string FileName { get; }

    /// <summary>
    /// Gets the content type.
    /// </summary>
    public string ContentType { get; }

    /// <summary>
    /// Gets the file content.
    /// </summary>
    public byte[] Content { get; }
}

/// <summary>
/// Resolves document templates from an external store.
/// </summary>
public interface IDocumentTemplateResolver
{
    /// <summary>
    /// Resolves a template by code.
    /// </summary>
    /// <param name="templateCode">The template code.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The resolved template.</returns>
    Task<DocumentTemplateDescriptor> ResolveAsync(string templateCode, CancellationToken cancellationToken = default);
}

/// <summary>
/// Renders template content to text.
/// </summary>
public interface IDocumentTemplateRenderer
{
    /// <summary>
    /// Renders template content using a document request.
    /// </summary>
    /// <param name="templateContent">The template content.</param>
    /// <param name="request">The render request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The rendered content.</returns>
    Task<string> RenderAsync(
        string templateContent,
        DocumentRenderRequest request,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Renders HTML content to PDF bytes.
/// </summary>
public interface IDocumentPdfRenderer
{
    /// <summary>
    /// Renders HTML content to PDF.
    /// </summary>
    /// <param name="html">The rendered HTML.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The PDF file bytes.</returns>
    Task<byte[]> RenderPdfAsync(string html, CancellationToken cancellationToken = default);
}

/// <summary>
/// Coordinates template resolution, variable rendering, and output file creation.
/// </summary>
public interface IDocumentRenderService
{
    /// <summary>
    /// Renders a document request.
    /// </summary>
    /// <param name="request">The render request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The rendered file result.</returns>
    Task<DocumentFileResult> RenderAsync(DocumentRenderRequest request, CancellationToken cancellationToken = default);
}
