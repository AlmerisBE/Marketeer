using Marketeer.Core;
using Marketeer.Features.Dashboard.Contracts;
using Marketeer.Features.Financials.Contracts;
using Marketeer.Features.Financials.Providers;
using Marketeer.Features.Financials.Services;
using Marketeer.Features.Financials.UI;
using Marketeer.Features.Localization.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Marketeer.Features.Financials;

public class FinancialsFeature : IFeatureModule {
    public void RegisterServices(IServiceCollection services) {
        services.AddSingleton<ILocalizationProvider, FinancialsLocalizationProvider>();
        services.AddSingleton<IFinancialService, FinancialService>();

        services.AddSingleton<FinancialsTab>();

        // Forward to INavigationNode instead of IDashboardTab
        services.AddSingleton<INavigationNode>(provider => provider.GetRequiredService<FinancialsTab>());
    }
}