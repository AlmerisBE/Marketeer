using Marketeer.Core.CharacterManagement.Commands;
using Marketeer.Core.CharacterManagement.Contracts;
using Marketeer.Core.Framework;
using Marketeer.UI.CharacterManagement.Providers;
using Marketeer.UI.CharacterManagement.UI;
using Marketeer.UI.Command.Contracts;
using Marketeer.UI.Dashboard.Contracts;
using Marketeer.UI.Localization.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Marketeer.UI.CharacterManagement;

public class CharacterManagementUiFeature : IFeatureModule {
    public void RegisterServices(IServiceCollection services) {
        services.AddSingleton<ILocalizationProvider, CharacterManagementLocalizationProvider>();

        // UI Presenters (Moved from Core)
        services.AddSingleton<IRetainerDataPresenter, RetainerDataPresenter>();
        services.AddSingleton<ICharacterSummaryView, CharacterSummaryView>();

        // Navigation Nodes
        services.AddSingleton<CharacterOverviewMenu>();
        services.AddSingleton<INavigationNode>(provider => provider.GetRequiredService<CharacterOverviewMenu>());

        // Commands
        services.AddSingleton<ICommand, CharacterListCommand>();
    }
}