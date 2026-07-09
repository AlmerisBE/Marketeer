using BasePlugin.Core;
using BasePlugin.Features.Command.Contracts;
using BasePlugin.Features.Configuration.Commands;
using BasePlugin.Features.Configuration.Contracts;
using BasePlugin.Features.Configuration.Services;
using BasePlugin.Features.Configuration.UI;
using Dalamud.Interface.Windowing;
using Microsoft.Extensions.DependencyInjection;

namespace BasePlugin.Features.Configuration;

public class ConfigurationFeature : IFeatureModule {
    public void RegisterServices(IServiceCollection services) {
        services.AddSingleton<IConfigurationService, ConfigurationService>();

        // Window registration
        services.AddSingleton<ConfigWindow>();
        services.AddSingleton<Window>(provider => provider.GetRequiredService<ConfigWindow>());

        services.AddSingleton<ICommand, ConfigCommand>();
    }
}