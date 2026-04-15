# Vktun.Engine.Excel

Reusable Excel import, export, template, and workflow engine for Vktun applications.

## Register services

```csharp
services.AddVktunExcelEngine(options =>
{
    options.AllowedRootDirectories.Add(importRoot);
});
```

## Export

```csharp
var request = new ExcelExportRequest
{
    FileName = "report.xlsx"
};

var sheet = new ExcelSheetDefinition
{
    Name = "Report",
    Rows = rows
};

sheet.Columns.Add(new ExcelColumnDefinition("Name", "Name"));
sheet.Columns.Add(new ExcelColumnDefinition("Amount", "Amount"));
request.Sheets.Add(sheet);

var bytes = await excelExportService.ExportAsync(request, cancellationToken);
```

## Import

```csharp
public sealed class ImportRow
{
    [ExcelColumn("Name", Required = true)]
    public string Name { get; set; } = string.Empty;

    [ExcelColumn("Amount")]
    public decimal Amount { get; set; }
}

var result = await excelImportService.ImportAsync<ImportRow>(stream, cancellationToken);
```
