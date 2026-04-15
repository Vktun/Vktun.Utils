using System.Globalization;
using System.Reflection;
using ClosedXML.Excel;

namespace Vktun.Engine.Excel;

/// <summary>
/// Default Excel import service based on ClosedXML.
/// </summary>
public sealed class DefaultExcelImportService : IExcelImportService
{
    /// <inheritdoc />
    public Task<ExcelImportResult<TRow>> ImportAsync<TRow>(
        Stream content,
        CancellationToken cancellationToken = default)
        where TRow : new()
    {
        ArgumentNullException.ThrowIfNull(content);
        cancellationToken.ThrowIfCancellationRequested();

        using var workbook = new XLWorkbook(content);
        var worksheet = workbook.Worksheets.FirstOrDefault()
            ?? throw new InvalidOperationException("The Excel workbook does not contain any worksheets.");

        var result = new ExcelImportResult<TRow>();
        var properties = GetImportProperties<TRow>();
        var headerMap = ReadHeaderMap(worksheet);
        var lastRowNumber = worksheet.LastRowUsed()?.RowNumber() ?? 1;

        for (var rowNumber = 2; rowNumber <= lastRowNumber; rowNumber++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (worksheet.Row(rowNumber).IsEmpty())
            {
                continue;
            }

            var row = new TRow();

            foreach (var property in properties)
            {
                if (!headerMap.TryGetValue(property.Header, out var columnNumber) &&
                    !headerMap.TryGetValue(property.Property.Name, out columnNumber))
                {
                    if (property.Required)
                    {
                        result.Errors.Add(new ExcelRowErrorDto
                        {
                            RowNumber = rowNumber,
                            ColumnName = property.Header,
                            Message = $"Required column '{property.Header}' was not found."
                        });
                    }

                    continue;
                }

                var cell = worksheet.Cell(rowNumber, columnNumber);
                var rawText = cell.GetFormattedString();
                if (string.IsNullOrWhiteSpace(rawText))
                {
                    if (property.Required)
                    {
                        result.Errors.Add(new ExcelRowErrorDto
                        {
                            RowNumber = rowNumber,
                            ColumnName = property.Header,
                            Message = $"Column '{property.Header}' is required."
                        });
                    }

                    continue;
                }

                try
                {
                    var converted = ConvertValue(cell, property.Property.PropertyType);
                    property.Property.SetValue(row, converted);
                }
                catch (Exception exception) when (exception is FormatException or InvalidCastException or ArgumentException)
                {
                    result.Errors.Add(new ExcelRowErrorDto
                    {
                        RowNumber = rowNumber,
                        ColumnName = property.Header,
                        Message = $"Column '{property.Header}' value '{rawText}' cannot be converted to {property.Property.PropertyType.Name}."
                    });
                }
            }

            result.Rows.Add(row);
        }

        return Task.FromResult(result);
    }

    private static Dictionary<string, int> ReadHeaderMap(IXLWorksheet worksheet)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var lastColumnNumber = worksheet.Row(1).LastCellUsed()?.Address.ColumnNumber ?? 0;

        for (var columnNumber = 1; columnNumber <= lastColumnNumber; columnNumber++)
        {
            var header = worksheet.Cell(1, columnNumber).GetFormattedString();
            if (!string.IsNullOrWhiteSpace(header))
            {
                map[header.Trim()] = columnNumber;
            }
        }

        return map;
    }

    private static IReadOnlyList<ImportProperty> GetImportProperties<TRow>()
    {
        return typeof(TRow)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(static property => property.SetMethod is not null)
            .Select(static property => new ImportProperty(
                property,
                property.GetCustomAttribute<ExcelColumnAttribute>()))
            .Where(static property => property.Attribute?.Ignored != true)
            .ToArray();
    }

    private static object? ConvertValue(IXLCell cell, Type targetType)
    {
        var nullableType = Nullable.GetUnderlyingType(targetType);
        var effectiveType = nullableType ?? targetType;
        var text = cell.GetFormattedString().Trim();

        if (nullableType is not null && string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        if (effectiveType == typeof(string))
        {
            return text;
        }

        if (effectiveType == typeof(Guid))
        {
            return Guid.Parse(text);
        }

        if (effectiveType == typeof(DateTime))
        {
            return cell.TryGetValue<DateTime>(out var dateTime)
                ? dateTime
                : DateTime.Parse(text, CultureInfo.CurrentCulture);
        }

        if (effectiveType == typeof(DateOnly))
        {
            if (cell.TryGetValue<DateTime>(out var dateTime))
            {
                return DateOnly.FromDateTime(dateTime);
            }

            return DateOnly.Parse(text, CultureInfo.CurrentCulture);
        }

        if (effectiveType == typeof(TimeOnly))
        {
            if (cell.TryGetValue<TimeSpan>(out var timeSpan))
            {
                return TimeOnly.FromTimeSpan(timeSpan);
            }

            return TimeOnly.Parse(text, CultureInfo.CurrentCulture);
        }

        if (effectiveType == typeof(bool))
        {
            if (text.Equals("1", StringComparison.OrdinalIgnoreCase) ||
                text.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                text.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
                text.Equals("y", StringComparison.OrdinalIgnoreCase) ||
                text.Equals("是", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (text.Equals("0", StringComparison.OrdinalIgnoreCase) ||
                text.Equals("false", StringComparison.OrdinalIgnoreCase) ||
                text.Equals("no", StringComparison.OrdinalIgnoreCase) ||
                text.Equals("n", StringComparison.OrdinalIgnoreCase) ||
                text.Equals("否", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return bool.Parse(text);
        }

        if (effectiveType.IsEnum)
        {
            return Enum.Parse(effectiveType, text, ignoreCase: true);
        }

        return Convert.ChangeType(text, effectiveType, CultureInfo.CurrentCulture);
    }

    private sealed record ImportProperty(PropertyInfo Property, ExcelColumnAttribute? Attribute)
    {
        public string Header => string.IsNullOrWhiteSpace(Attribute?.Name) ? Property.Name : Attribute!.Name!;

        public bool Required => Attribute?.Required ?? false;
    }
}
