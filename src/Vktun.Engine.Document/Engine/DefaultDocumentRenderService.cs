using System.Text;

namespace Vktun.Engine.Document;

/// <summary>
/// Default document rendering service.
/// </summary>
public sealed class DefaultDocumentRenderService(
    IDocumentTemplateRenderer templateRenderer,
    IEnumerable<IDocumentTemplateResolver> templateResolvers,
    IEnumerable<IDocumentPdfRenderer> pdfRenderers) : IDocumentRenderService
{
    /// <inheritdoc />
    public async Task<DocumentFileResult> RenderAsync(
        DocumentRenderRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var template = await ResolveTemplateAsync(request, cancellationToken).ConfigureAwait(false);
        var renderedContent = await templateRenderer
            .RenderAsync(template.Content, request, cancellationToken)
            .ConfigureAwait(false);

        var outputContentType = string.IsNullOrWhiteSpace(request.OutputContentType)
            ? template.ContentType
            : request.OutputContentType;

        if (DocumentContentTypeMatcher.IsPdf(outputContentType))
        {
            var pdfRenderer = pdfRenderers.FirstOrDefault();
            if (pdfRenderer is null)
            {
                throw new InvalidOperationException(
                    "PDF output was requested, but no IDocumentPdfRenderer is registered.");
            }

            var pdfContent = await pdfRenderer
                .RenderPdfAsync(renderedContent, cancellationToken)
                .ConfigureAwait(false);

            return new DocumentFileResult(
                DocumentFileNames.EnsureExtension(request.FileName, DocumentContentTypes.Pdf),
                DocumentContentTypes.Pdf,
                pdfContent);
        }

        return new DocumentFileResult(
            DocumentFileNames.EnsureExtension(request.FileName, outputContentType),
            outputContentType,
            Encoding.UTF8.GetBytes(renderedContent));
    }

    private async Task<DocumentTemplateDescriptor> ResolveTemplateAsync(
        DocumentRenderRequest request,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.TemplateContent))
        {
            return new DocumentTemplateDescriptor
            {
                Code = request.TemplateCode ?? string.Empty,
                Content = request.TemplateContent,
                ContentType = DocumentContentTypes.Html
            };
        }

        if (string.IsNullOrWhiteSpace(request.TemplateCode))
        {
            throw new InvalidOperationException(
                "Either TemplateContent or TemplateCode must be supplied for document rendering.");
        }

        var resolver = templateResolvers.FirstOrDefault();
        if (resolver is null)
        {
            throw new InvalidOperationException(
                "TemplateCode was supplied, but no IDocumentTemplateResolver is registered.");
        }

        return await resolver.ResolveAsync(request.TemplateCode, cancellationToken).ConfigureAwait(false);
    }
}
