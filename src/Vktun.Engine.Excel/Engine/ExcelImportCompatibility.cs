using Microsoft.Extensions.Options;

namespace Vktun.Engine.Excel;

/// <summary>
/// Options for controlled file-system based Excel imports.
/// </summary>
public sealed class ExcelImportCompatibilityOptions
{
    /// <summary>
    /// Gets allowed root directories for physical file imports.
    /// </summary>
    public IList<string> AllowedRootDirectories { get; } = [];

    /// <summary>
    /// Gets allowed file extensions.
    /// </summary>
    public ISet<string> AllowedExtensions { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".xlsx"
    };

    /// <summary>
    /// Gets or sets the maximum import file size in bytes.
    /// </summary>
    public long MaxFileSizeBytes { get; set; } = 50 * 1024 * 1024;
}

/// <summary>
/// Opens import files only from configured roots and allowed extensions.
/// </summary>
public sealed class ControlledExcelImportFileResolver : IExcelImportFileResolver
{
    private readonly ExcelImportCompatibilityOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="ControlledExcelImportFileResolver"/> class.
    /// </summary>
    /// <param name="options">The compatibility options.</param>
    public ControlledExcelImportFileResolver(IOptions<ExcelImportCompatibilityOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
    }

    /// <inheritdoc />
    public Task<Stream> OpenReadAsync(ExcelImportFileInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        if (input.Content is not null)
        {
            return Task.FromResult(input.Content);
        }

        if (string.IsNullOrWhiteSpace(input.FilePath))
        {
            throw new InvalidOperationException("Import input must contain either content or a file path.");
        }

        var fullPath = Path.GetFullPath(input.FilePath);
        EnsureAllowedExtension(fullPath);
        EnsureAllowedRoot(fullPath);

        var fileInfo = new FileInfo(fullPath);
        if (!fileInfo.Exists)
        {
            throw new FileNotFoundException("Excel import file was not found.", fullPath);
        }

        if (_options.MaxFileSizeBytes > 0 && fileInfo.Length > _options.MaxFileSizeBytes)
        {
            throw new InvalidOperationException($"Excel import file exceeds the configured size limit of {_options.MaxFileSizeBytes} bytes.");
        }

        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult(stream);
    }

    private void EnsureAllowedExtension(string fullPath)
    {
        var extension = Path.GetExtension(fullPath);
        if (!_options.AllowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException($"Excel import extension '{extension}' is not allowed.");
        }
    }

    private void EnsureAllowedRoot(string fullPath)
    {
        if (_options.AllowedRootDirectories.Count == 0)
        {
            return;
        }

        var allowed = _options.AllowedRootDirectories
            .Where(static root => !string.IsNullOrWhiteSpace(root))
            .Select(static root => Path.GetFullPath(root))
            .Any(root =>
            {
                var normalizedRoot = root.EndsWith(Path.DirectorySeparatorChar)
                    ? root
                    : root + Path.DirectorySeparatorChar;

                return fullPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase);
            });

        if (!allowed)
        {
            throw new InvalidOperationException("Excel import file is outside of the configured allowed roots.");
        }
    }
}

/// <summary>
/// Default compatibility import service.
/// </summary>
public sealed class DefaultExcelImportCompatibilityService : IExcelImportCompatibilityService
{
    private readonly IExcelImportFileResolver _fileResolver;
    private readonly IExcelImportService _importService;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultExcelImportCompatibilityService"/> class.
    /// </summary>
    /// <param name="fileResolver">The import file resolver.</param>
    /// <param name="importService">The import service.</param>
    public DefaultExcelImportCompatibilityService(
        IExcelImportFileResolver fileResolver,
        IExcelImportService importService)
    {
        _fileResolver = fileResolver ?? throw new ArgumentNullException(nameof(fileResolver));
        _importService = importService ?? throw new ArgumentNullException(nameof(importService));
    }

    /// <inheritdoc />
    public async Task<ExcelImportResult<TRow>> ImportAsync<TRow>(
        ExcelImportFileInput input,
        CancellationToken cancellationToken = default)
        where TRow : new()
    {
        await using var stream = await _fileResolver.OpenReadAsync(input, cancellationToken).ConfigureAwait(false);
        return await _importService.ImportAsync<TRow>(stream, cancellationToken).ConfigureAwait(false);
    }
}
