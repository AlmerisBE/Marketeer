using Marketeer.API.Dashboard.Contracts;
using Marketeer.API.Features;
using Marketeer.API.Financials.Contracts;
using Marketeer.API.Localization.Contracts;
using Marketeer.Core.Financials.Services;
using Marketeer.UI.Financials.Providers;
using Marketeer.UI.Financials.UI;
using Microsoft.Extensions.DependencyInjection;

namespace Marketeer.Core.Financials;

public class FinancialsFeature : IFeatureModule {
    public void RegisterServices(IServiceCollection services) {
        // UI Providers
        services.AddSingleton<ILocalizationProvider, FinancialsLocalizationProvider>();

        // Core implementations
        services.AddSingleton<IFinancialService, FinancialService>();

        // UI implementations
        services.AddSingleton<FinancialsMenu>();
        services.AddSingleton<INavigationNode>(provider => provider.GetRequiredService<FinancialsMenu>());
    }
}