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

        services.AddSingleton<UniversalisConfigMenu>();
        services.AddSingleton<INavigationNode>(provider => provider.GetRequiredService<UniversalisConfigMenu>());

        services.AddSingleton<CompetitionConfigMenu>();
        services.AddSingleton<INavigationNode>(provider => provider.GetRequiredService<CompetitionConfigMenu>());

        services.AddSingleton<ThemeConfigMenu>();
        services.AddSingleton<INavigationNode>(provider => provider.GetRequiredService<ThemeConfigMenu>());

        services.AddSingleton<HotkeyConfigMenu>();
        services.AddSingleton<INavigationNode>(provider => provider.GetRequiredService<HotkeyConfigMenu>());

        services.AddSingleton<OtherConfigMenu>();
        services.AddSingleton<INavigationNode>(provider => provider.GetRequiredService<OtherConfigMenu>());

        services.AddSingleton<ICommand, ConfigCommand>();
    }
}