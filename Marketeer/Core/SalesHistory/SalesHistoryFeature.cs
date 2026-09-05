using Marketeer.Core.Framework;
using Marketeer.Core.SalesHistory.Commands;
using Marketeer.Core.SalesHistory.Contracts;
using Marketeer.Core.SalesHistory.Providers;
using Marketeer.Core.SalesHistory.Services;
using Marketeer.Core.SalesHistory.UI;
using Marketeer.UI.Command.Contracts;
using Marketeer.UI.Dashboard.Contracts;
using Marketeer.UI.Localization.Contracts;
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