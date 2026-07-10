using Marketeer.Core;
using Marketeer.Features.Dashboard.Contracts;
using Marketeer.Features.RetainerTracking.Contracts;
using Marketeer.Features.RetainerTracking.Presenters;
using Marketeer.Features.RetainerTracking.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Marketeer.Features.RetainerTracking;

public class RetainerTrackingFeature : IFeatureModule {
    public void RegisterServices(IServiceCollection services) {
        services.AddSingleton<IRetainerTrackerService, RetainerTrackerService>();
        services.AddSingleton<IRetainerDataPresenter, RetainerDataPresenter>();
    }
}