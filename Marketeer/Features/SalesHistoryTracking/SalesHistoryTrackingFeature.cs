using Marketeer.Core;
using Marketeer.Features.SalesHistoryTracking.Contracts;
using Marketeer.Features.SalesHistoryTracking.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Marketeer.Features.SalesHistoryTracking;

public class SalesHistoryTrackingFeature : IFeatureModule {
    public void RegisterServices(IServiceCollection services) {
        services.AddSingleton<ISalesAnalysisService, SalesAnalysisService>();
        services.AddSingleton<ISalesHistoryScraper, SalesHistoryScraper>();
    }
}