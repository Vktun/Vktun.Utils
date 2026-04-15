using System.Globalization;
using ClosedXML.Excel;

namespace Vktun.Engine.Excel;

/// <summary>
/// Default Excel export service based on ClosedXML.
/// </summary>
public sealed class DefaultExcelExportService : IExcelExportService
{
    private readonly IExcelTemplateResolver? _templateResolver;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultExcelExportService"/> class.
    /// </summary>
    /// <param name="templateResolver">The optional template resolver.</param>
    public DefaultExcelExportService(IExcelTemplateResolver? templateResolver = null)
    {
        _templateResolver = templateResolver;
    }

    /// <inheritdoc />
    public async Task<byte[]> ExportAsync(ExcelExportRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var templatePath = await ResolveTemplatePathAsync(request, cancellationToken).ConfigureAwait(false);
        using var workbook = !string.IsNullOrWhiteSpace(templatePath)
            ? new XLWorkbook(templatePath)
            : new XLWorkbook();

        if (!string.IsNullOrWhiteSpace(templatePath))
        {
            RenderTemplateValues(workbook, request.TemplateValues);
        }

        if (request.Sheets.Count > 0)
        {
            WriteSheets(workbook, request);
        }

        await using var output = new MemoryStream();
        workbook.SaveAs(output);
        return output.ToArray();
    }

    private async Task<string?> ResolveTemplatePathAsync(ExcelExportRequest request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.TemplatePath))
        {
            return request.TemplatePath;
        }

        if (!string.IsNullOrWhiteSpace(request.TemplateCode) && _templateResolver is not null)
        {
            return await _templateResolver.ResolvePathAsync(request.TemplateCode, cancellationToken).ConfigureAwait(false);
        }

        return null;
    }

    private static void RenderTemplateValues(XLWorkbook workbook, IDictionary<string, object?> values)
    {
        if (values.Count == 0)
        {
            return;
        }

        foreach (var worksheet in workbook.Worksheets)
        {
            foreach (var cell in worksheet.CellsUsed())
            {
                var text = cell.GetString();
                if (string.IsNullOrEmpty(text) || !text.Contains("{{", StringComparison.Ordinal))
                {
                    continue;
                }

                foreach (var (key, value) in values)
                {
                    text = text.Replace(
                        "{{" + key + "}}",
                        Convert.ToString(value, CultureInfo.CurrentCulture),
                        StringComparison.OrdinalIgnoreCase);
                }

                cell.SetValue(text);
            }
        }
    }

    private static void WriteSheets(XLWorkbook workbook, ExcelExportRequest request)
    {
        var usedNames = new HashSet<string>(
            workbook.Worksheets.Select(static worksheet => worksheet.Name),
            StringComparer.OrdinalIgnoreCase);

        foreach (var sheet in request.Sheets)
        {
            var rows = sheet.Rows.ToArray();
            var maxRows = request.MaxRowsPerSheet.GetValueOrDefault(rows.Length == 0 ? 1 : rows.Length);
            var chunks = maxRows > 0 ? rows.Chunk(maxRows).ToArray() : [rows];

            for (var chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
            {
                var suffix = chunks.Length == 1 ? string.Empty : $" {chunkIndex + 1}";
                var sheetName = CreateUniqueSheetName(sheet.Name + suffix, usedNames);
                var worksheet = workbook.Worksheets.Add(sheetName);
                WriteSheet(worksheet, sheet.Columns, chunks[chunkIndex]);
            }
        }
    }

    private static void WriteSheet(IXLWorksheet worksheet, IList<ExcelColumnDefinition> columns, object?[] rows)
    {
        for (var columnIndex = 0; columnIndex < columns.Count; columnIndex++)
        {
            var column = columns[columnIndex];
            var cell = worksheet.Cell(1, columnIndex + 1);
            cell.SetValue(string.IsNullOrWhiteSpace(column.Header) ? column.Key : column.Header);
            cell.Style.Font.Bold = true;

            if (column.Width is > 0)
            {
                worksheet.Column(columnIndex + 1).Width = column.Width.Value;
            }

            if (column.Hidden)
            {
                worksheet.Column(columnIndex + 1).Hide();
            }
        }

        for (var rowIndex = 0; rowIndex < rows.Length; rowIndex++)
        {
            var row = rows[rowIndex];
            for (var columnIndex = 0; columnIndex < columns.Count; columnIndex++)
            {
                var column = columns[columnIndex];
                var cell = worksheet.Cell(rowIndex + 2, columnIndex + 1);
                SetCellValue(cell, column.GetValue(row));

                if (!string.IsNullOrWhiteSpace(column.Format))
                {
                    cell.Style.NumberFormat.Format = column.Format;
                }
            }
        }

        if (columns.Count > 0)
        {
            worksheet.SheetView.FreezeRows(1);
            worksheet.Range(1, 1, Math.Max(rows.Length + 1, 1), columns.Count).SetAutoFilter();

            foreach (var column in worksheet.Columns(1, columns.Count).Where(static column => !column.IsHidden))
            {
                column.AdjustToContents();
            }
        }
    }

    private static void SetCellValue(IXLCell cell, object? value)
    {
        switch (value)
        {
            case null:
                cell.Clear(XLClearOptions.Contents);
                break;
            case string text:
                cell.SetValue(text);
                break;
            case DateTime dateTime:
                cell.SetValue(dateTime);
                break;
            case DateOnly dateOnly:
                cell.SetValue(dateOnly.ToDateTime(TimeOnly.MinValue));
                break;
            case TimeOnly timeOnly:
                cell.SetValue(timeOnly.ToTimeSpan());
                break;
            case bool boolean:
                cell.SetValue(boolean);
                break;
            case byte number:
                cell.SetValue(number);
                break;
            case short number:
                cell.SetValue(number);
                break;
            case int number:
                cell.SetValue(number);
                break;
            case long number:
                cell.SetValue(number);
                break;
            case float number:
                cell.SetValue(number);
                break;
            case double number:
                cell.SetValue(number);
                break;
            case decimal number:
                cell.SetValue(number);
                break;
            default:
                cell.SetValue(Convert.ToString(value, CultureInfo.CurrentCulture) ?? string.Empty);
                break;
        }
    }

    private static string CreateUniqueSheetName(string requestedName, ISet<string> usedNames)
    {
        var baseName = SanitizeSheetName(requestedName);
        var name = baseName;
        var index = 1;

        while (!usedNames.Add(name))
        {
            var suffix = $" ({index++})";
            name = baseName.Length + suffix.Length > 31
                ? baseName[..(31 - suffix.Length)] + suffix
                : baseName + suffix;
        }

        return name;
    }

    private static string SanitizeSheetName(string name)
    {
        var sanitized = string.Join("_", (string.IsNullOrWhiteSpace(name) ? "Sheet" : name).Split(
            ['\\', '/', '?', '*', '[', ']', ':'],
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

        return sanitized.Length <= 31 ? sanitized : sanitized[..31];
    }
}
