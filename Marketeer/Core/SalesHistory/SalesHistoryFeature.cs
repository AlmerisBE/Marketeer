using Marketeer.API.Command.Contracts;
using Marketeer.API.Dashboard.Contracts;
using Marketeer.API.Features;
using Marketeer.API.Localization.Contracts;
using Marketeer.API.SalesHistory.Contracts;
using Marketeer.Core.SalesHistory.Services;
using Marketeer.UI.SalesHistory.Commands;
using Marketeer.UI.SalesHistory.Providers;
using Marketeer.UI.SalesHistory.Services;
using Marketeer.UI.SalesHistory.UI;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Marketeer.Core.SalesHistory;

public class SalesHistoryFeature : IFeatureModule {
    public void RegisterServices(IServiceCollection services) {
        services.AddSingleton<ILocalizationProvider, SalesHistoryLocalizationProvider>();

        services.AddSingleton<ISalesAnalysisService, SalesAnalysisService>();
        services.AddSingleton<ISalesHistoryScraper, SalesHistoryScraper>();
        services.AddSingleton<ISalesRepository, SalesRepository>();
        services.AddSingleton<ISalesDataProvider, SalesDataProvider>();
        services.AddSingleton<IItemResolverService, ItemResolverService>();
        services.AddSingleton<ISalesInferenceService, SalesInferenceService>();
        services.AddSingleton<ISalesStatisticsService, SalesStatisticsService>();

        services.AddSingleton<ISalesScannerService, SalesScannerService>();

        services.AddSingleton<ISalesDataPresenter, SalesDataPresenter>();
        services.AddSingleton<ICommand, HistoryCommand>();

        services.AddSingleton<SalesMenu>();
        services.AddSingleton<INavigationNode>(provider => provider.GetRequiredService<SalesMenu>());

        services.AddSingleton<SalesStatisticsMenu>();
        services.AddSingleton<INavigationNode>(provider => provider.GetRequiredService<SalesStatisticsMenu>());
    }

    public void Initialize(IServiceProvider provider) {
        var salesScanner = provider.GetRequiredService<ISalesScannerService>();
        salesScanner.Enable();
    }
}