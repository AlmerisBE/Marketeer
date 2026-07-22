using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Marketeer.API.CompetitionTracking.Contracts;
using Marketeer.API.Dashboard.Contracts;
using Marketeer.API.GameInterop.Contracts;
using Marketeer.API.SalesHistory.Contracts;
using Marketeer.API.UiInterop.Contracts;
using Marketeer.Core.Command.Services;
using Marketeer.Core.Dependencies;
using Marketeer.UI.Configuration.UI;
using Marketeer.UI.Dashboard.UI;
using Microsoft.Extensions.DependencyInjection;

namespace Marketeer;

public sealed class Plugin : IDalamudPlugin {
    public string Name => "Marketeer";

    private ServiceProvider serviceProvider;
    private IDalamudPluginInterface pluginInterface;
    private WindowSystem windowSystem;

    public Plugin(
        IDalamudPluginInterface pluginInterface,
        IChatGui chatGui,
        IGameGui gameGui,
        ICommandManager commandManager,
        IClientState clientState,
        IPluginLog pluginLog,
        IObjectTable objectTable,
        IFramework framework,
        IDataManager dataManager,
        ICondition condition,
        IAddonLifecycle addonLifecycle,
        ITextureProvider textureProvider) {

        this.pluginInterface = pluginInterface;
        this.windowSystem = new WindowSystem("Marketeer");

        var services = new ServiceCollection();

        // 1. Register Dalamud Services
        services.AddSingleton(this.pluginInterface);
        services.AddSingleton(chatGui);
        services.AddSingleton(gameGui);
        services.AddSingleton(commandManager);
        services.AddSingleton(clientState);
        services.AddSingleton(pluginLog);
        services.AddSingleton(objectTable);
        services.AddSingleton(framework);
        services.AddSingleton(dataManager);
        services.AddSingleton(condition);
        services.AddSingleton(addonLifecycle);
        services.AddSingleton(textureProvider);

        // 2. Discover and register all features automatically
        services.AddPluginFeatures();

        // 3. Build the container
        this.serviceProvider = services.BuildServiceProvider();

        // 4. Initialize Core Systems
        this.serviceProvider.GetRequiredService<CommandDispatcher>();

        // Start Window Tracking
        var windowTracker = this.serviceProvider.GetRequiredService<IWindowTrackerService>();
        windowTracker.EnableTracking();

        // Enable the Sales Scanner to listen for RetainerItemHistory
        var salesScanner = this.serviceProvider.GetRequiredService<ISalesScannerService>();
        salesScanner.Enable();

        var monitorService = this.serviceProvider.GetRequiredService<ICompetitionMonitorService>();
        monitorService.StartMonitoring();

        // Force instantiation of the GameEventService so it starts listening immediately
        this.serviceProvider.GetRequiredService<IGameEventService>();

        // 5. Initialize Window System
        var windows = this.serviceProvider.GetServices<Window>();
        foreach (var window in windows) {
            this.windowSystem.AddWindow(window);
        }

        // 6. Hook UI events
        this.pluginInterface.UiBuilder.Draw += this.windowSystem.Draw;
        this.pluginInterface.UiBuilder.OpenConfigUi += this.OnOpenConfigUi;
    }

    private void OnOpenConfigUi() {
        var navService = this.serviceProvider.GetService<IDashboardNavigationService>();
        var configMenu = this.serviceProvider.GetService<ConfigMenu>();
        var dashboardWindow = this.serviceProvider.GetService<DashboardWindow>();

        if (navService != null && configMenu != null && dashboardWindow != null) {
            navService.NavigateTo(configMenu);
            dashboardWindow.IsOpen = true;
        }
    }

    public void Dispose() {
        this.pluginInterface.UiBuilder.Draw -= this.windowSystem.Draw;
        this.pluginInterface.UiBuilder.OpenConfigUi -= this.OnOpenConfigUi;

        var monitorService = this.serviceProvider.GetService<ICompetitionMonitorService>();
        monitorService?.StopMonitoring();

        this.windowSystem.RemoveAllWindows();
        this.serviceProvider.Dispose();
    }
}