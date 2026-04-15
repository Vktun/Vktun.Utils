# Vktun.Engine.Document

`Vktun.Engine.Document` is the reusable document rendering engine package for Vktun applications.

## Scope

- HTML template variable rendering.
- Document file result model.
- Template resolver abstraction for database-backed template stores.
- Optional PDF renderer abstraction.
- File name and content type normalization.

Business-specific template content, contract rules, invoice rules, and repair-order rules should stay in the owning business module.

## Usage

```csharp
services.AddVktunDocumentEngine();
```

```csharp
var result = await documentRenderService.RenderAsync(new DocumentRenderRequest
{
    TemplateContent = "<h1>{{ CustomerName }}</h1><p>{CreatedAt:yyyy-MM-dd}</p>",
    Values =
    {
        ["CustomerName"] = "Vktun",
        ["CreatedAt"] = DateTimeOffset.Now
    },
    FileName = "contract",
    OutputContentType = DocumentContentTypes.Html
});
```

The default renderer supports `{Name}`, `{Name:format}`, `{{ Name }}`, and `{{ Name:format }}` placeholders.
