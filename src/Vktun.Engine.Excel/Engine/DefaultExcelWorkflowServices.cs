namespace Vktun.Engine.Excel;

/// <summary>
/// Default template service for Excel import definitions.
/// </summary>
public sealed class DefaultExcelTemplateService : IExcelTemplateService
{
    private readonly IExcelExportService _exportService;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultExcelTemplateService"/> class.
    /// </summary>
    /// <param name="exportService">The export service.</param>
    public DefaultExcelTemplateService(IExcelExportService exportService)
    {
        _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
    }

    /// <inheritdoc />
    public async Task<ExcelFileDto> GenerateImportTemplateAsync<TRow>(
        IExcelImportDefinition<TRow> definition,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(definition);

        var columns = definition.Columns
            .Select(static column => new ExcelColumnDefinition
            {
                Key = column.Key,
                Header = column.Header,
                Hidden = column.Hidden,
                Format = column.Format
            })
            .ToArray();

        var exampleRow = definition.Columns.Any(static column => !string.IsNullOrWhiteSpace(column.Example))
            ? new Dictionary<string, object?>(
                definition.Columns.Select(static column => KeyValuePair.Create(column.Key, (object?)column.Example)),
                StringComparer.OrdinalIgnoreCase)
            : null;

        var sheet = new ExcelSheetDefinition
        {
            Name = string.IsNullOrWhiteSpace(definition.SheetName) ? "Template" : definition.SheetName,
            Rows = exampleRow is null ? [] : [exampleRow]
        };

        foreach (var column in columns)
        {
            sheet.Columns.Add(column);
        }

        var request = new ExcelExportRequest
        {
            FileName = string.IsNullOrWhiteSpace(definition.TemplateFileName)
                ? "template.xlsx"
                : definition.TemplateFileName
        };
        request.Sheets.Add(sheet);

        var content = await _exportService.ExportAsync(request, cancellationToken).ConfigureAwait(false);
        return new ExcelFileDto
        {
            FileName = request.FileName,
            ContentType = ExcelContentTypes.Xlsx,
            Content = content
        };
    }
}

/// <summary>
/// Default typed Excel export orchestrator.
/// </summary>
public sealed class DefaultExcelExportOrchestrator : IExcelExportOrchestrator
{
    private readonly IExcelExportService _exportService;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultExcelExportOrchestrator"/> class.
    /// </summary>
    /// <param name="exportService">The export service.</param>
    public DefaultExcelExportOrchestrator(IExcelExportService exportService)
    {
        _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
    }

    /// <inheritdoc />
    public async Task<ExcelFileDto> ExportAsync<TQuery, TRow>(
        IExcelExportDefinition<TQuery, TRow> definition,
        TQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(definition);

        var rows = await definition.QueryAsync(query, cancellationToken).ConfigureAwait(false);
        var sheet = new ExcelSheetDefinition
        {
            Name = string.IsNullOrWhiteSpace(definition.SheetName) ? "Sheet1" : definition.SheetName,
            Rows = rows.Cast<object?>()
        };

        foreach (var column in definition.Columns)
        {
            sheet.Columns.Add(column);
        }

        var request = new ExcelExportRequest
        {
            FileName = string.IsNullOrWhiteSpace(definition.FileName) ? "export.xlsx" : definition.FileName,
            MaxRowsPerSheet = definition.MaxRowsPerSheet
        };
        request.Sheets.Add(sheet);

        var content = await _exportService.ExportAsync(request, cancellationToken).ConfigureAwait(false);
        return new ExcelFileDto
        {
            FileName = request.FileName,
            ContentType = ExcelContentTypes.Xlsx,
            Content = content
        };
    }
}

/// <summary>
/// Default typed Excel import orchestrator.
/// </summary>
public sealed class DefaultExcelImportOrchestrator : IExcelImportOrchestrator
{
    private readonly IExcelImportService _importService;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultExcelImportOrchestrator"/> class.
    /// </summary>
    /// <param name="importService">The import service.</param>
    public DefaultExcelImportOrchestrator(IExcelImportService importService)
    {
        _importService = importService ?? throw new ArgumentNullException(nameof(importService));
    }

    /// <inheritdoc />
    public async Task<ExcelImportPreviewDto<TRow>> PreviewAsync<TRow>(
        IExcelImportDefinition<TRow> definition,
        Stream content,
        CancellationToken cancellationToken = default)
        where TRow : new()
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(content);

        var importResult = await _importService.ImportAsync<TRow>(content, cancellationToken).ConfigureAwait(false);
        var validationErrors = await definition.ValidateAsync(importResult.Rows.ToArray(), cancellationToken).ConfigureAwait(false);

        var preview = new ExcelImportPreviewDto<TRow>();
        foreach (var row in importResult.Rows)
        {
            preview.Rows.Add(row);
        }

        foreach (var error in importResult.Errors.Concat(validationErrors))
        {
            preview.Errors.Add(error);
        }

        return preview;
    }

    /// <inheritdoc />
    public Task<TResult> CommitAsync<TRow, TResult>(
        IExcelImportCommitter<TRow, TResult> committer,
        IReadOnlyList<TRow> rows,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(committer);
        ArgumentNullException.ThrowIfNull(rows);
        return committer.CommitAsync(rows, cancellationToken);
    }
}
