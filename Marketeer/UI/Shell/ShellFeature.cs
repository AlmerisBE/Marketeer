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

        // Abstractions et Services Globaux
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<CommandDispatcher>();
        services.AddSingleton<HotkeyTrackerService>();

        // Fenêtre principale et vue par défaut
        services.AddSingleton<MainWindow>();
        services.AddSingleton<Window>(provider => provider.GetRequiredService<MainWindow>());
        services.AddSingleton<INavigationNode, WelcomeMenu>();

        // Commandes Shell
        services.AddSingleton<ICommand, MainCommand>();
    }

    public void Initialize(IServiceProvider provider) {
        // Démarre l'écoute des commandes chat
        provider.GetRequiredService<CommandDispatcher>();

        // Démarre l'écoute du raccourci clavier global
        provider.GetRequiredService<HotkeyTrackerService>();
    }
}