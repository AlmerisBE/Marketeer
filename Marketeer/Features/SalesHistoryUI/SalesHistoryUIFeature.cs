using Marketeer.Core;
using Marketeer.Features.Dashboard.Contracts;
using Marketeer.Features.Localization.Contracts;
using Marketeer.Features.SalesHistoryUI.Contracts;
using Marketeer.Features.SalesHistoryUI.Providers;
using Marketeer.Features.SalesHistoryUI.Services;
using Marketeer.Features.SalesHistoryUI.UI;
using Microsoft.Extensions.DependencyInjection;

namespace Marketeer.Features.SalesHistoryUI;

public class SalesHistoryUIFeature : IFeatureModule {
    public void RegisterServices(IServiceCollection services) {
        services.AddSingleton<ILocalizationProvider, SalesUiLocalizationProvider>();
        services.AddSingleton<ISalesDataPresenter, SalesDataPresenter>();
        services.AddSingleton<IDashboardTab, SalesTab>();
    }
}