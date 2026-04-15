namespace Vktun.Engine.Excel;

/// <summary>
/// Defines a typed Excel export workflow.
/// </summary>
/// <typeparam name="TQuery">The query type.</typeparam>
/// <typeparam name="TRow">The exported row type.</typeparam>
public interface IExcelExportDefinition<in TQuery, TRow>
{
    /// <summary>
    /// Gets the exported file name.
    /// </summary>
    string FileName { get; }

    /// <summary>
    /// Gets the exported sheet name.
    /// </summary>
    string SheetName { get; }

    /// <summary>
    /// Gets the maximum row count per sheet.
    /// </summary>
    int? MaxRowsPerSheet => null;

    /// <summary>
    /// Gets the exported columns.
    /// </summary>
    IReadOnlyList<ExcelTypedColumnDefinition<TRow>> Columns { get; }

    /// <summary>
    /// Queries rows for export.
    /// </summary>
    /// <param name="query">The export query.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The exported rows.</returns>
    Task<IReadOnlyList<TRow>> QueryAsync(TQuery query, CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines a typed Excel import workflow.
/// </summary>
/// <typeparam name="TRow">The imported row type.</typeparam>
public interface IExcelImportDefinition<TRow>
{
    /// <summary>
    /// Gets the template file name.
    /// </summary>
    string TemplateFileName { get; }

    /// <summary>
    /// Gets the template sheet name.
    /// </summary>
    string SheetName { get; }

    /// <summary>
    /// Gets the import columns.
    /// </summary>
    IReadOnlyList<ExcelColumnDescriptorDto> Columns { get; }

    /// <summary>
    /// Validates imported rows.
    /// </summary>
    /// <param name="rows">The imported rows.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The validation errors.</returns>
    Task<IReadOnlyList<ExcelRowErrorDto>> ValidateAsync(
        IReadOnlyList<TRow> rows,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Commits imported rows to business storage.
/// </summary>
/// <typeparam name="TRow">The imported row type.</typeparam>
/// <typeparam name="TResult">The commit result type.</typeparam>
public interface IExcelImportCommitter<in TRow, TResult>
{
    /// <summary>
    /// Commits imported rows.
    /// </summary>
    /// <param name="rows">The imported rows.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The commit result.</returns>
    Task<TResult> CommitAsync(IReadOnlyList<TRow> rows, CancellationToken cancellationToken = default);
}

/// <summary>
/// Generates Excel templates.
/// </summary>
public interface IExcelTemplateService
{
    /// <summary>
    /// Generates an import template.
    /// </summary>
    /// <typeparam name="TRow">The imported row type.</typeparam>
    /// <param name="definition">The import definition.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The generated template file.</returns>
    Task<ExcelFileDto> GenerateImportTemplateAsync<TRow>(
        IExcelImportDefinition<TRow> definition,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Orchestrates typed exports.
/// </summary>
public interface IExcelExportOrchestrator
{
    /// <summary>
    /// Exports rows with a typed definition.
    /// </summary>
    /// <typeparam name="TQuery">The query type.</typeparam>
    /// <typeparam name="TRow">The row type.</typeparam>
    /// <param name="definition">The export definition.</param>
    /// <param name="query">The export query.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The exported file.</returns>
    Task<ExcelFileDto> ExportAsync<TQuery, TRow>(
        IExcelExportDefinition<TQuery, TRow> definition,
        TQuery query,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Orchestrates typed imports.
/// </summary>
public interface IExcelImportOrchestrator
{
    /// <summary>
    /// Previews imported rows.
    /// </summary>
    /// <typeparam name="TRow">The row type.</typeparam>
    /// <param name="definition">The import definition.</param>
    /// <param name="content">The Excel content.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The import preview.</returns>
    Task<ExcelImportPreviewDto<TRow>> PreviewAsync<TRow>(
        IExcelImportDefinition<TRow> definition,
        Stream content,
        CancellationToken cancellationToken = default)
        where TRow : new();

    /// <summary>
    /// Commits imported rows.
    /// </summary>
    /// <typeparam name="TRow">The row type.</typeparam>
    /// <typeparam name="TResult">The commit result type.</typeparam>
    /// <param name="committer">The import committer.</param>
    /// <param name="rows">The rows to commit.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The commit result.</returns>
    Task<TResult> CommitAsync<TRow, TResult>(
        IExcelImportCommitter<TRow, TResult> committer,
        IReadOnlyList<TRow> rows,
        CancellationToken cancellationToken = default);
}
