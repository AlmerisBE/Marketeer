using Marketeer.API.Command.Contracts;
using Marketeer.API.Configuration.Contracts;
using Marketeer.API.Dashboard.Contracts;
using Marketeer.API.Features;
using Marketeer.API.Localization.Contracts;
using Marketeer.Core.Configuration.Services;
using Marketeer.UI.Configuration.Commands;
using Marketeer.UI.Configuration.Providers;
using Marketeer.UI.Configuration.UI;
using Microsoft.Extensions.DependencyInjection;

namespace Marketeer.Core.Configuration;

public class ConfigurationFeature : IFeatureModule {
    public void RegisterServices(IServiceCollection services) {
        // Providers
        services.AddSingleton<ILocalizationProvider, ConfigurationLocalizationProvider>();

        // Core implementations
        services.AddSingleton<IConfigurationService, ConfigurationService>();

        // UI Implementations
        services.AddSingleton<ConfigMenu>();
        services.AddSingleton<INavigationNode>(provider => provider.GetRequiredService<ConfigMenu>());

        // Commands
        services.AddSingleton<ICommand, ConfigCommand>();
    }
}