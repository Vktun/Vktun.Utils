namespace Vktun.Engine.Excel;

/// <summary>
/// Marks a property as an Excel column.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class ExcelColumnAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExcelColumnAttribute"/> class.
    /// </summary>
    public ExcelColumnAttribute()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExcelColumnAttribute"/> class.
    /// </summary>
    /// <param name="name">The displayed column name.</param>
    public ExcelColumnAttribute(string name)
    {
        Name = name;
    }

    /// <summary>
    /// Gets or sets the displayed column name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the column order.
    /// </summary>
    public int Order { get; set; } = int.MaxValue;

    /// <summary>
    /// Gets or sets a value indicating whether this column is required during import.
    /// </summary>
    public bool Required { get; set; }

    /// <summary>
    /// Gets or sets a sample value for generated templates.
    /// </summary>
    public string? Example { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this column is hidden.
    /// </summary>
    public bool Hidden { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this property should be ignored.
    /// </summary>
    public bool Ignored { get; set; }

    /// <summary>
    /// Gets or sets an Excel number format string.
    /// </summary>
    public string? Format { get; set; }

    /// <summary>
    /// Gets or sets the desired column width.
    /// </summary>
    public double Width { get; set; }
}

/// <summary>
/// Represents an Excel file returned by the engine.
/// </summary>
public sealed class ExcelFileDto
{
    /// <summary>
    /// Gets or sets the file name.
    /// </summary>
    public string FileName { get; set; } = "export.xlsx";

    /// <summary>
    /// Gets or sets the content type.
    /// </summary>
    public string ContentType { get; set; } = ExcelContentTypes.Xlsx;

    /// <summary>
    /// Gets or sets the file content.
    /// </summary>
    public byte[] Content { get; set; } = [];
}

/// <summary>
/// Describes an import file input.
/// </summary>
public sealed class ExcelImportFileInput
{
    /// <summary>
    /// Gets or sets the original file name.
    /// </summary>
    public string? FileName { get; set; }

    /// <summary>
    /// Gets or sets the optional template code.
    /// </summary>
    public string? TemplateCode { get; set; }

    /// <summary>
    /// Gets or sets a physical file path.
    /// </summary>
    public string? FilePath { get; set; }

    /// <summary>
    /// Gets or sets direct file content.
    /// </summary>
    public Stream? Content { get; set; }
}

/// <summary>
/// Describes a column in an Excel template.
/// </summary>
public sealed class ExcelColumnDescriptorDto
{
    /// <summary>
    /// Gets or sets the field key.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the displayed header.
    /// </summary>
    public string Header { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the data type text.
    /// </summary>
    public string? DataType { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the column is required.
    /// </summary>
    public bool Required { get; set; }

    /// <summary>
    /// Gets or sets the sample value.
    /// </summary>
    public string? Example { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the column is hidden.
    /// </summary>
    public bool Hidden { get; set; }

    /// <summary>
    /// Gets or sets an Excel number format string.
    /// </summary>
    public string? Format { get; set; }
}

/// <summary>
/// Describes an Excel template.
/// </summary>
public sealed class ExcelTemplateDescriptorDto
{
    /// <summary>
    /// Gets or sets the template code.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the template display name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the template direction.
    /// </summary>
    public ExcelTemplateDirection Direction { get; set; }

    /// <summary>
    /// Gets the template columns.
    /// </summary>
    public IList<ExcelColumnDescriptorDto> Columns { get; } = [];
}

/// <summary>
/// Describes an import row error.
/// </summary>
public sealed class ExcelRowErrorDto
{
    /// <summary>
    /// Gets or sets the one-based row number in the worksheet.
    /// </summary>
    public int RowNumber { get; set; }

    /// <summary>
    /// Gets or sets the column key or header.
    /// </summary>
    public string? ColumnName { get; set; }

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Represents imported rows and validation errors.
/// </summary>
/// <typeparam name="TRow">The row type.</typeparam>
public sealed class ExcelImportResult<TRow>
{
    /// <summary>
    /// Gets the imported rows.
    /// </summary>
    public IList<TRow> Rows { get; } = [];

    /// <summary>
    /// Gets the import errors.
    /// </summary>
    public IList<ExcelRowErrorDto> Errors { get; } = [];

    /// <summary>
    /// Gets a value indicating whether the import has errors.
    /// </summary>
    public bool HasErrors => Errors.Count > 0;
}

/// <summary>
/// Represents a preview of imported rows.
/// </summary>
/// <typeparam name="TRow">The row type.</typeparam>
public sealed class ExcelImportPreviewDto<TRow>
{
    /// <summary>
    /// Gets the preview rows.
    /// </summary>
    public IList<TRow> Rows { get; } = [];

    /// <summary>
    /// Gets the row errors.
    /// </summary>
    public IList<ExcelRowErrorDto> Errors { get; } = [];

    /// <summary>
    /// Gets a value indicating whether the preview has errors.
    /// </summary>
    public bool HasErrors => Errors.Count > 0;
}
