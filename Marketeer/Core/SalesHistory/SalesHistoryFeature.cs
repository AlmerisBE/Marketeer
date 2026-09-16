using Marketeer.Core.Framework;
using Marketeer.Core.SalesHistory.Contracts;
using Marketeer.Core.SalesHistory.Services;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Marketeer.Core.SalesHistory;

public class SalesHistoryFeature : IFeatureModule {
    public void RegisterServices(IServiceCollection services) {
        services.AddSingleton<ISalesAnalysisService, SalesAnalysisService>();
        services.AddSingleton<ISalesHistoryScraper, SalesHistoryScraper>();
        services.AddSingleton<ISalesRepository, SalesRepository>();
        services.AddSingleton<ISalesDataProvider, SalesDataProvider>();
        services.AddSingleton<IItemResolverService, ItemResolverService>();
        services.AddSingleton<ISalesInferenceService, SalesInferenceService>();
        services.AddSingleton<ISalesStatisticsService, SalesStatisticsService>();

        services.AddSingleton<ISalesScannerService, SalesScannerService>();
    }

    public void Initialize(IServiceProvider provider) {
        var salesScanner = provider.GetRequiredService<ISalesScannerService>();
        salesScanner.Enable();
    }
}