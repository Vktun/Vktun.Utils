using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Vktun.Engine.Document;
using Xunit;

namespace Vktun.Engine.Document.Tests;

public sealed class DocumentEngineTests
{
    [Fact]
    public async Task Renderer_supports_dictionary_model_and_formatting()
    {
        var renderer = new DefaultDocumentTemplateRenderer();
        var result = await renderer.RenderAsync(
            "<h1>{{ CustomerName }}</h1><p>{CreatedAt:yyyy-MM-dd}</p><p>{Amount:N2}</p>",
            new DocumentRenderRequest
            {
                CultureName = "zh-CN",
                Values =
                {
                    ["CustomerName"] = "Vktun",
                    ["CreatedAt"] = new DateTime(2026, 4, 15),
                    ["Amount"] = 1234.5m
                }
            });

        Assert.Contains("<h1>Vktun</h1>", result);
        Assert.Contains("<p>2026-04-15</p>", result);
        Assert.Contains("<p>1,234.50</p>", result);
    }

    [Fact]
    public async Task Renderer_can_keep_missing_placeholders()
    {
        var renderer = new DefaultDocumentTemplateRenderer();
        var result = await renderer.RenderAsync(
            "Hello {{ Missing }}",
            new DocumentRenderRequest
            {
                MissingVariableBehavior = DocumentMissingVariableBehavior.KeepPlaceholder
            });

        Assert.Equal("Hello {{ Missing }}", result);
    }

    [Fact]
    public async Task Renderer_can_throw_for_missing_placeholders()
    {
        var renderer = new DefaultDocumentTemplateRenderer();

        await Assert.ThrowsAsync<KeyNotFoundException>(() => renderer.RenderAsync(
            "Hello {{ Missing }}",
            new DocumentRenderRequest
            {
                MissingVariableBehavior = DocumentMissingVariableBehavior.Throw
            }));
    }

    [Fact]
    public async Task Render_service_uses_registered_resolver()
    {
        var services = new ServiceCollection()
            .AddVktunDocumentEngine()
            .AddSingleton<IDocumentTemplateResolver>(
                new StubTemplateResolver("<h1>{{Title}}</h1>"))
            .BuildServiceProvider();

        var renderService = services.GetRequiredService<IDocumentRenderService>();
        var result = await renderService.RenderAsync(new DocumentRenderRequest
        {
            TemplateCode = "contract",
            FileName = "contract",
            Values = { ["Title"] = "合同" }
        });

        Assert.Equal("contract.html", result.FileName);
        Assert.Equal(DocumentContentTypes.Html, result.ContentType);
        Assert.Equal("<h1>合同</h1>", Encoding.UTF8.GetString(result.Content));
    }

    [Fact]
    public async Task Render_service_requires_pdf_renderer_for_pdf_output()
    {
        var services = new ServiceCollection()
            .AddVktunDocumentEngine()
            .BuildServiceProvider();

        var renderService = services.GetRequiredService<IDocumentRenderService>();

        await Assert.ThrowsAsync<InvalidOperationException>(() => renderService.RenderAsync(new DocumentRenderRequest
        {
            TemplateContent = "<h1>PDF</h1>",
            FileName = "document",
            OutputContentType = DocumentContentTypes.Pdf
        }));
    }

    private sealed class StubTemplateResolver(string content) : IDocumentTemplateResolver
    {
        public Task<DocumentTemplateDescriptor> ResolveAsync(
            string templateCode,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new DocumentTemplateDescriptor
            {
                Code = templateCode,
                Content = content,
                ContentType = DocumentContentTypes.Html
            });
        }
    }
}
