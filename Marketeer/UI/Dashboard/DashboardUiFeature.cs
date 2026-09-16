using Dalamud.Interface.Windowing;
using Marketeer.Core.Framework;
using Marketeer.UI.Command.Contracts;
using Marketeer.UI.Dashboard.Commands;
using Marketeer.UI.Dashboard.Providers;
using Marketeer.UI.Dashboard.UI;
using Marketeer.UI.Localization.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Marketeer.UI.Dashboard;

public class DashboardUiFeature : IFeatureModule {
    public void RegisterServices(IServiceCollection services) {
        services.AddSingleton<ILocalizationProvider, DashboardLocalizationProvider>();

        services.AddSingleton<DashboardWindow>();
        services.AddSingleton<Window>(provider => provider.GetRequiredService<DashboardWindow>());

        services.AddSingleton<ICommand, MainCommand>();
    }
}