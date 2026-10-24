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

        // Global Abstractions and Services
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<CommandDispatcher>();
        services.AddSingleton<HotkeyTrackerService>();

        // Main Window and Default View
        services.AddSingleton<MainWindow>();
        services.AddSingleton<Window>(provider => provider.GetRequiredService<MainWindow>());

        services.AddSingleton<WelcomeMenu>();
        services.AddSingleton<INavigationNode>(provider => provider.GetRequiredService<WelcomeMenu>());

        services.AddSingleton<AboutMenu>();
        services.AddSingleton<INavigationNode>(provider => provider.GetRequiredService<AboutMenu>());

        services.AddSingleton<UniversalisStatusBarItem>();
        services.AddSingleton<IStatusBarProvider>(provider => provider.GetRequiredService<UniversalisStatusBarItem>());

        // Consolidated Global Configurations (Shell)
        services.AddSingleton<SystemConfigMenu>();
        services.AddSingleton<INavigationNode>(provider => provider.GetRequiredService<SystemConfigMenu>());

        services.AddSingleton<InterfaceConfigMenu>();
        services.AddSingleton<INavigationNode>(provider => provider.GetRequiredService<InterfaceConfigMenu>());

        // Shell Commands
        services.AddSingleton<ICommand, MainCommand>();
        services.AddSingleton<ICommand, ConfigCommand>();
    }

    public void Initialize(IServiceProvider provider) {
        provider.GetRequiredService<CommandDispatcher>();
        provider.GetRequiredService<HotkeyTrackerService>();
    }
}