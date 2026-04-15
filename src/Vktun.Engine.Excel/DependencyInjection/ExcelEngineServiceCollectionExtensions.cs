using Microsoft.Extensions.DependencyInjection;

namespace Vktun.Engine.Excel;

/// <summary>
/// Registers Vktun Excel engine services.
/// </summary>
public static class ExcelEngineServiceCollectionExtensions
{
    /// <summary>
    /// Adds the default Excel engine services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">An optional compatibility options delegate.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddVktunExcelEngine(
        this IServiceCollection services,
        Action<ExcelImportCompatibilityOptions>? configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<ExcelImportCompatibilityOptions>();

        if (configureOptions is not null)
        {
            services.Configure(configureOptions);
        }

        services.AddSingleton<IExcelExportService>(serviceProvider =>
            new DefaultExcelExportService(serviceProvider.GetService<IExcelTemplateResolver>()));
        services.AddSingleton<IExcelImportService, DefaultExcelImportService>();
        services.AddSingleton<IExcelTemplateService, DefaultExcelTemplateService>();
        services.AddSingleton<IExcelExportOrchestrator, DefaultExcelExportOrchestrator>();
        services.AddSingleton<IExcelImportOrchestrator, DefaultExcelImportOrchestrator>();
        services.AddSingleton<IExcelImportFileResolver, ControlledExcelImportFileResolver>();
        services.AddSingleton<IExcelImportCompatibilityService, DefaultExcelImportCompatibilityService>();

        return services;
    }
}
