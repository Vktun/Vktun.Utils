using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Vktun.Engine.Document;

/// <summary>
/// Provides dependency injection registration helpers for the document engine.
/// </summary>
public static class DocumentEngineServiceCollectionExtensions
{
    /// <summary>
    /// Registers the default document engine services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection.</returns>
    public static IServiceCollection AddVktunDocumentEngine(this IServiceCollection services)
    {
        services.TryAddSingleton<IDocumentTemplateRenderer, DefaultDocumentTemplateRenderer>();
        services.TryAddSingleton<IDocumentRenderService, DefaultDocumentRenderService>();
        return services;
    }
}
