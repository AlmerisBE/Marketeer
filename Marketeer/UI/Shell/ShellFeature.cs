using Dalamud.Interface.Windowing;
using Marketeer.Core.Framework;
using Marketeer.UI.Localization.Contracts;
using Marketeer.UI.Shell.Commands;
using Marketeer.UI.Shell.Contracts;
using Marketeer.UI.Shell.Providers;
using Marketeer.UI.Shell.Services;
using Marketeer.UI.Shell.UI;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Marketeer.UI.Shell;

public class ShellFeature : IFeatureModule {
    public void RegisterServices(IServiceCollection services) {
        services.AddSingleton<ILocalizationProvider, ShellLocalizationProvider>();

        // Abstractions de navigation
        services.AddSingleton<INavigationService, NavigationService>();

        // Fenêtre principale et vue par défaut
        services.AddSingleton<MainWindow>();
        services.AddSingleton<Window>(provider => provider.GetRequiredService<MainWindow>());
        services.AddSingleton<INavigationNode, WelcomeMenu>();

        // Commande d'ouverture
        services.AddSingleton<ICommand, MainCommand>();

        services.AddSingleton<HotkeyTrackerService>();
    }

    public void Initialize(IServiceProvider provider) {
        provider.GetRequiredService<CommandDispatcher>();
        provider.GetRequiredService<HotkeyTrackerService>();
    }
}