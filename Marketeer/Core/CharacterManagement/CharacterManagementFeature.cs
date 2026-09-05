using Marketeer.Core.CharacterManagement.Commands;
using Marketeer.Core.CharacterManagement.Contracts;
using Marketeer.Core.CharacterManagement.Providers;
using Marketeer.Core.CharacterManagement.Services;
using Marketeer.Core.CharacterManagement.UI;
using Marketeer.Core.Framework;
using Marketeer.UI.Command.Contracts;
using Marketeer.UI.Dashboard.Contracts;
using Marketeer.UI.Localization.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Marketeer.Core.CharacterManagement;

public class CharacterManagementFeature : IFeatureModule {
    public void RegisterServices(IServiceCollection services) {
        services.AddSingleton<ILocalizationProvider, CharacterManagementLocalizationProvider>();

        // Core Tracking
        services.AddSingleton<ICharacterTrackerService, CharacterTrackerService>();
        services.AddSingleton<IRetainerTrackerService, RetainerTrackerService>();

        // UI & Presenters
        services.AddSingleton<IRetainerDataPresenter, RetainerDataPresenter>();
        services.AddSingleton<ICharacterSummaryView, CharacterSummaryView>();

        // Nodes registration
        services.AddSingleton<CharacterOverviewMenu>();
        services.AddSingleton<INavigationNode>(provider => provider.GetRequiredService<CharacterOverviewMenu>());

        services.AddSingleton<ICommand, CharacterListCommand>();
    }
}