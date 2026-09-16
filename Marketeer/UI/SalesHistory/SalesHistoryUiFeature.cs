using Marketeer.Core.Framework;
using Marketeer.Core.SalesHistory.Contracts;
using Marketeer.UI.Command.Contracts;
using Marketeer.UI.Dashboard.Contracts;
using Marketeer.UI.Localization.Contracts;
using Marketeer.UI.SalesHistory.Commands;
using Marketeer.UI.SalesHistory.Presenters;
using Marketeer.UI.SalesHistory.Providers;
using Marketeer.UI.SalesHistory.UI;
using Microsoft.Extensions.DependencyInjection;

namespace Marketeer.UI.SalesHistory;

public class SalesHistoryUiFeature : IFeatureModule {
    public void RegisterServices(IServiceCollection services) {
        services.AddSingleton<ILocalizationProvider, SalesHistoryLocalizationProvider>();

        services.AddSingleton<ISalesDataPresenter, SalesDataPresenter>();
        services.AddSingleton<ICommand, HistoryCommand>();

        services.AddSingleton<SalesMenu>();
        services.AddSingleton<INavigationNode>(provider => provider.GetRequiredService<SalesMenu>());

        services.AddSingleton<SalesStatisticsMenu>();
        services.AddSingleton<INavigationNode>(provider => provider.GetRequiredService<SalesStatisticsMenu>());
    }
}