using Marketeer.Core.Financials.Contracts;
using Marketeer.Core.Financials.Services;
using Marketeer.Core.Framework;
using Marketeer.UI.Financials.Providers;
using Marketeer.UI.Financials.UI;
using Marketeer.UI.Localization.Contracts;
using Marketeer.UI.Shell.Contracts;
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