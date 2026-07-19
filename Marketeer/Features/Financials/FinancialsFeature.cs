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

        // Register the concrete type so ConfigWindow can resolve it directly[cite: 1]
        services.AddSingleton<FinancialsTab>();

        // Register the interface forwarding to the concrete singleton so DashboardWindow can resolve it dynamically[cite: 1]
        services.AddSingleton<IDashboardTab>(provider => provider.GetRequiredService<FinancialsTab>());
    }
}