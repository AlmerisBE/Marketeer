using Dalamud.Interface.Windowing;
using Marketeer.Core;
using Marketeer.Features.Command.Contracts;
using Marketeer.Features.Dashboard.Commands;
using Marketeer.Features.Dashboard.UI;
using Microsoft.Extensions.DependencyInjection;

namespace Marketeer.Features.Dashboard;

public class DashboardFeature : IFeatureModule {
    public void RegisterServices(IServiceCollection services) {
        services.AddSingleton<DashboardWindow>();
        services.AddSingleton<Window>(provider => provider.GetRequiredService<DashboardWindow>());

        services.AddSingleton<ICommand, MainCommand>();
    }
}