using Marketeer.Core.Framework;
using Marketeer.UI.Configuration.Commands;
using Marketeer.UI.Configuration.Providers;
using Marketeer.UI.Configuration.UI;
using Marketeer.UI.Localization.Contracts;
using Marketeer.UI.Shell.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Marketeer.UI.Configuration;

public class ConfigurationUiFeature : IFeatureModule {
    public void RegisterServices(IServiceCollection services) {
        services.AddSingleton<ILocalizationProvider, ConfigurationLocalizationProvider>();

        services.AddSingleton<ConfigMenu>();
        services.AddSingleton<INavigationNode>(provider => provider.GetRequiredService<ConfigMenu>());

        services.AddSingleton<ICommand, ConfigCommand>();
    }
}