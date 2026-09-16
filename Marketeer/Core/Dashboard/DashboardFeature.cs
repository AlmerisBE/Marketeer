using Marketeer.Core.Dashboard.Services;
using Marketeer.Core.Framework;
using Marketeer.UI.Dashboard.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Marketeer.Core.Dashboard;

public class DashboardFeature : IFeatureModule {
    public void RegisterServices(IServiceCollection services) {
        // Uniquement la gestion de l'état de navigation
        services.AddSingleton<IDashboardNavigationService, DashboardNavigationService>();
    }
}