using System.Reflection;

namespace Vktun.Engine.Excel;

/// <summary>
/// Identifies the preferred engine implementation for an Excel export request.
/// </summary>
public enum ExcelEngineHint
{
    /// <summary>
    /// Uses the engine default for the current request.
    /// </summary>
    Auto = 0,

    /// <summary>
    /// Uses the ClosedXML implementation.
    /// </summary>
    ClosedXml = 1
}

/// <summary>
/// Defines whether an Excel template is used for import or export.
/// </summary>
public enum ExcelTemplateDirection
{
    /// <summary>
    /// Template direction is not specified.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Template is used for exports.
    /// </summary>
    Export = 1,

    /// <summary>
    /// Template is used for imports.
    /// </summary>
    Import = 2
}

/// <summary>
/// Common Excel content types.
/// </summary>
public static class ExcelContentTypes
{
    /// <summary>
    /// Content type for OpenXML workbook files.
    /// </summary>
    public const string Xlsx = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    /// <summary>
    /// Content type for legacy Excel workbook files.
    /// </summary>
    public const string Xls = "application/vnd.ms-excel";
}

/// <summary>
/// Describes a complete Excel export request.
/// </summary>
public sealed class ExcelExportRequest
{
    /// <summary>
    /// Gets or sets the exported file name.
    /// </summary>
    public string FileName { get; set; } = "export.xlsx";

    /// <summary>
    /// Gets or sets the requested engine implementation.
    /// </summary>
    public ExcelEngineHint EngineHint { get; set; } = ExcelEngineHint.Auto;

    /// <summary>
    /// Gets or sets the maximum row count per sheet before rows are split to another sheet.
    /// </summary>
    public int? MaxRowsPerSheet { get; set; }

    /// <summary>
    /// Gets or sets the template code to resolve with <see cref="IExcelTemplateResolver"/>.
    /// </summary>
    public string? TemplateCode { get; set; }

    /// <summary>
    /// Gets or sets the physical template path used for template rendering.
    /// </summary>
    public string? TemplatePath { get; set; }

    /// <summary>
    /// Gets or sets template placeholder values.
    /// </summary>
    public IDictionary<string, object?> TemplateValues { get; set; } =
        new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets the sheet definitions to export.
    /// </summary>
    public IList<ExcelSheetDefinition> Sheets { get; } = [];
}

/// <summary>
/// Describes one Excel worksheet to export.
/// </summary>
public sealed class ExcelSheetDefinition
{
    /// <summary>
    /// Gets or sets the worksheet name.
    /// </summary>
    public string Name { get; set; } = "Sheet1";

    /// <summary>
    /// Gets the column definitions.
    /// </summary>
    public IList<ExcelColumnDefinition> Columns { get; } = [];

    /// <summary>
    /// Gets or sets the data rows.
    /// </summary>
    public IEnumerable<object?> Rows { get; set; } = [];
}

/// <summary>
/// Describes one exported column.
/// </summary>
public class ExcelColumnDefinition
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExcelColumnDefinition"/> class.
    /// </summary>
    public ExcelColumnDefinition()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExcelColumnDefinition"/> class.
    /// </summary>
    /// <param name="key">The field or property key.</param>
    /// <param name="header">The displayed column header.</param>
    public ExcelColumnDefinition(string key, string header)
    {
        Key = key;
        Header = header;
    }

    /// <summary>
    /// Gets or sets the field or property key.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the displayed column header.
    /// </summary>
    public string Header { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the column should be hidden.
    /// </summary>
    public bool Hidden { get; set; }

    /// <summary>
    /// Gets or sets the desired column width.
    /// </summary>
    public double? Width { get; set; }

    /// <summary>
    /// Gets or sets an Excel number format string.
    /// </summary>
    public string? Format { get; set; }

    /// <summary>
    /// Gets or sets a value accessor for dynamic rows.
    /// </summary>
    public Func<object?, object?>? ValueAccessor { get; set; }

    /// <summary>
    /// Gets a value from the supplied row.
    /// </summary>
    /// <param name="row">The row object.</param>
    /// <returns>The column value.</returns>
    public virtual object? GetValue(object? row)
    {
        return ValueAccessor is not null ? ValueAccessor(row) : ExcelReflection.GetValue(row, Key);
    }
}

/// <summary>
/// Describes a typed exported column.
/// </summary>
/// <typeparam name="TRow">The row type.</typeparam>
public sealed class ExcelTypedColumnDefinition<TRow> : ExcelColumnDefinition
{
    /// <summary>
    /// Gets or sets a typed value accessor.
    /// </summary>
    public Func<TRow, object?>? TypedValueAccessor { get; set; }

    /// <inheritdoc />
    public override object? GetValue(object? row)
    {
        if (row is TRow typedRow && TypedValueAccessor is not null)
        {
            return TypedValueAccessor(typedRow);
        }

        return base.GetValue(row);
    }
}

/// <summary>
/// Builds <see cref="ExcelExportRequest"/> instances.
/// </summary>
public sealed class ExcelExportRequestBuilder
{
    private readonly ExcelExportRequest _request = new();

    /// <summary>
    /// Sets the exported file name.
    /// </summary>
    /// <param name="fileName">The file name.</param>
    /// <returns>The current builder.</returns>
    public ExcelExportRequestBuilder WithFileName(string fileName)
    {
        _request.FileName = string.IsNullOrWhiteSpace(fileName)
            ? throw new ArgumentException("File name cannot be empty.", nameof(fileName))
            : fileName;

        return this;
    }

    /// <summary>
    /// Sets the maximum row count per sheet.
    /// </summary>
    /// <param name="maxRowsPerSheet">The maximum row count.</param>
    /// <returns>The current builder.</returns>
    public ExcelExportRequestBuilder WithMaxRowsPerSheet(int? maxRowsPerSheet)
    {
        if (maxRowsPerSheet is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxRowsPerSheet), "Maximum rows per sheet must be positive.");
        }

        _request.MaxRowsPerSheet = maxRowsPerSheet;
        return this;
    }

    /// <summary>
    /// Sets the physical template path.
    /// </summary>
    /// <param name="templatePath">The template path.</param>
    /// <returns>The current builder.</returns>
    public ExcelExportRequestBuilder WithTemplatePath(string templatePath)
    {
        _request.TemplatePath = string.IsNullOrWhiteSpace(templatePath)
            ? throw new ArgumentException("Template path cannot be empty.", nameof(templatePath))
            : templatePath;

        return this;
    }

    /// <summary>
    /// Adds a template value.
    /// </summary>
    /// <param name="key">The placeholder key.</param>
    /// <param name="value">The placeholder value.</param>
    /// <returns>The current builder.</returns>
    public ExcelExportRequestBuilder WithTemplateValue(string key, object? value)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Template value key cannot be empty.", nameof(key));
        }

        _request.TemplateValues[key] = value;
        return this;
    }

    /// <summary>
    /// Adds a worksheet.
    /// </summary>
    /// <param name="sheet">The sheet definition.</param>
    /// <returns>The current builder.</returns>
    public ExcelExportRequestBuilder AddSheet(ExcelSheetDefinition sheet)
    {
        ArgumentNullException.ThrowIfNull(sheet);
        _request.Sheets.Add(sheet);
        return this;
    }

    /// <summary>
    /// Creates the configured request.
    /// </summary>
    /// <returns>The export request.</returns>
    public ExcelExportRequest Build()
    {
        return _request;
    }
}

/// <summary>
/// Describes a typed export profile.
/// </summary>
/// <typeparam name="TRow">The row type.</typeparam>
public interface IExcelExportProfile<TRow>
{
    /// <summary>
    /// Gets the exported sheet name.
    /// </summary>
    string SheetName { get; }

    /// <summary>
    /// Gets the exported columns.
    /// </summary>
    IReadOnlyList<ExcelTypedColumnDefinition<TRow>> Columns { get; }
}

/// <summary>
/// Exports Excel files.
/// </summary>
public interface IExcelExportService
{
    /// <summary>
    /// Exports an Excel workbook.
    /// </summary>
    /// <param name="request">The export request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The exported workbook bytes.</returns>
    Task<byte[]> ExportAsync(ExcelExportRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Imports Excel rows into typed objects.
/// </summary>
public interface IExcelImportService
{
    /// <summary>
    /// Imports rows from the first worksheet.
    /// </summary>
    /// <typeparam name="TRow">The row type.</typeparam>
    /// <param name="content">The Excel file content.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The import result.</returns>
    Task<ExcelImportResult<TRow>> ImportAsync<TRow>(Stream content, CancellationToken cancellationToken = default)
        where TRow : new();
}

/// <summary>
/// Resolves template paths by code.
/// </summary>
public interface IExcelTemplateResolver
{
    /// <summary>
    /// Resolves a physical template path.
    /// </summary>
    /// <param name="templateCode">The template code.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The template path, or <see langword="null"/> when no template exists.</returns>
    Task<string?> ResolvePathAsync(string templateCode, CancellationToken cancellationToken = default);
}

/// <summary>
/// Opens Excel import files.
/// </summary>
public interface IExcelImportFileResolver
{
    /// <summary>
    /// Opens an import file for reading.
    /// </summary>
    /// <param name="input">The import file input.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The readable stream.</returns>
    Task<Stream> OpenReadAsync(ExcelImportFileInput input, CancellationToken cancellationToken = default);
}

/// <summary>
/// Provides compatibility import from configured file paths.
/// </summary>
public interface IExcelImportCompatibilityService
{
    /// <summary>
    /// Imports a typed file.
    /// </summary>
    /// <typeparam name="TRow">The row type.</typeparam>
    /// <param name="input">The file input.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The import result.</returns>
    Task<ExcelImportResult<TRow>> ImportAsync<TRow>(ExcelImportFileInput input, CancellationToken cancellationToken = default)
        where TRow : new();
}

/// <summary>
/// Creates column definitions from typed row models.
/// </summary>
public static class ExcelColumnDefinitionFactory
{
    /// <summary>
    /// Creates typed column definitions from public readable properties.
    /// </summary>
    /// <typeparam name="TRow">The row type.</typeparam>
    /// <returns>The column definitions.</returns>
    public static IReadOnlyList<ExcelTypedColumnDefinition<TRow>> FromType<TRow>()
    {
        return typeof(TRow)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(static property => property.GetMethod is not null)
            .Select(static property => new
            {
                Property = property,
                Attribute = property.GetCustomAttribute<ExcelColumnAttribute>()
            })
            .Where(static item => item.Attribute?.Ignored != true)
            .OrderBy(static item => item.Attribute?.Order ?? int.MaxValue)
            .ThenBy(static item => item.Property.MetadataToken)
            .Select(static item => new ExcelTypedColumnDefinition<TRow>
            {
                Key = item.Property.Name,
                Header = string.IsNullOrWhiteSpace(item.Attribute?.Name) ? item.Property.Name : item.Attribute!.Name!,
                Hidden = item.Attribute?.Hidden ?? false,
                Format = item.Attribute?.Format,
                Width = item.Attribute?.Width > 0 ? item.Attribute.Width : null,
                TypedValueAccessor = row => item.Property.GetValue(row)
            })
            .ToArray();
    }

    /// <summary>
    /// Creates import descriptors from public writable properties.
    /// </summary>
    /// <typeparam name="TRow">The row type.</typeparam>
    /// <returns>The import descriptors.</returns>
    public static IReadOnlyList<ExcelColumnDescriptorDto> DescriptorsFromType<TRow>()
    {
        return typeof(TRow)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(static property => property.SetMethod is not null)
            .Select(static property => new
            {
                Property = property,
                Attribute = property.GetCustomAttribute<ExcelColumnAttribute>()
            })
            .Where(static item => item.Attribute?.Ignored != true)
            .OrderBy(static item => item.Attribute?.Order ?? int.MaxValue)
            .ThenBy(static item => item.Property.MetadataToken)
            .Select(static item => new ExcelColumnDescriptorDto
            {
                Key = item.Property.Name,
                Header = string.IsNullOrWhiteSpace(item.Attribute?.Name) ? item.Property.Name : item.Attribute!.Name!,
                Required = item.Attribute?.Required ?? false,
                Example = item.Attribute?.Example,
                Hidden = item.Attribute?.Hidden ?? false,
                Format = item.Attribute?.Format,
                DataType = Nullable.GetUnderlyingType(item.Property.PropertyType)?.Name ?? item.Property.PropertyType.Name
            })
            .ToArray();
    }
}

internal static class ExcelReflection
{
    public static object? GetValue(object? row, string key)
    {
        if (row is null || string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        if (row is IReadOnlyDictionary<string, object?> readOnlyDictionary &&
            readOnlyDictionary.TryGetValue(key, out var readOnlyValue))
        {
            return readOnlyValue;
        }

        if (row is IDictionary<string, object?> dictionary &&
            dictionary.TryGetValue(key, out var value))
        {
            return value;
        }

        var property = row.GetType().GetProperty(
            key,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);

        return property?.GetValue(row);
    }
}
