using Dalamud.Interface.Windowing;
using Marketeer.Core;
using Marketeer.Features.Command.Contracts;
using Marketeer.Features.Configuration.Commands;
using Marketeer.Features.Configuration.Contracts;
using Marketeer.Features.Configuration.Services;
using Marketeer.Features.Configuration.UI;
using Microsoft.Extensions.DependencyInjection;

namespace Marketeer.Features.Configuration;

public class ConfigurationFeature : IFeatureModule {
    public void RegisterServices(IServiceCollection services) {
        services.AddSingleton<IConfigurationService, ConfigurationService>();

        services.AddSingleton<ConfigWindow>();
        services.AddSingleton<Window>(provider => provider.GetRequiredService<ConfigWindow>());

        services.AddSingleton<ICommand, ConfigCommand>();
    }
}