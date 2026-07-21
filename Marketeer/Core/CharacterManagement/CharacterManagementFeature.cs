using Marketeer.API.CharacterManagement.Contracts;
using Marketeer.API.Command.Contracts;
using Marketeer.API.Dashboard.Contracts;
using Marketeer.API.Features;
using Marketeer.API.Localization.Contracts;
using Marketeer.Core.CharacterManagement.Services;
using Marketeer.UI.CharacterManagement.Commands;
using Marketeer.UI.CharacterManagement.Providers;
using Marketeer.UI.CharacterManagement.UI;
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