using Dalamud.Interface.Windowing;
using Marketeer.API.Command.Contracts;
using Marketeer.API.Configuration.Contracts;
using Marketeer.API.Features;
using Marketeer.Core.Configuration.Services;
using Marketeer.UI.Configuration.Commands;
using Marketeer.UI.Configuration.UI;
using Microsoft.Extensions.DependencyInjection;

namespace Marketeer.Core.Configuration;

public class ConfigurationFeature : IFeatureModule {
    public void RegisterServices(IServiceCollection services) {
        // Core implementations
        services.AddSingleton<IConfigurationService, ConfigurationService>();

        // UI Implementations
        services.AddSingleton<ConfigWindow>();
        services.AddSingleton<Window>(provider => provider.GetRequiredService<ConfigWindow>());

        // Commands
        services.AddSingleton<ICommand, ConfigCommand>();
    }
}