using Marketeer.Core.Configuration.Commands;
using Marketeer.Core.Configuration.Contracts;
using Marketeer.Core.Configuration.Providers;
using Marketeer.Core.Configuration.Services;
using Marketeer.Core.Configuration.UI;
using Marketeer.Core.Framework;
using Marketeer.UI.Command.Contracts;
using Marketeer.UI.Dashboard.Contracts;
using Marketeer.UI.Localization.Contracts;
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