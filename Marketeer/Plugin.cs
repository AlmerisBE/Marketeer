using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Marketeer.Core;
using Marketeer.Features.Command.Services;
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
        ICommandManager commandManager,
        IClientState clientState,
        IPluginLog pluginLog,
        IObjectTable objectTable,
        IFramework framework,
        IDataManager dataManager) {

        this.pluginInterface = pluginInterface;
        this.windowSystem = new WindowSystem("Marketeer");

        var services = new ServiceCollection();

        // 1. Register Dalamud Services
        services.AddSingleton(this.pluginInterface);
        services.AddSingleton(chatGui);
        services.AddSingleton(commandManager);
        services.AddSingleton(clientState);
        services.AddSingleton(pluginLog);
        services.AddSingleton(objectTable);
        services.AddSingleton(framework);
        services.AddSingleton(dataManager);

        // 2. Discover and register all features automatically
        services.AddPluginFeatures();

        // 3. Build the container
        this.serviceProvider = services.BuildServiceProvider();

        // 4. Initialize Core Systems
        this.serviceProvider.GetRequiredService<CommandDispatcher>();

        // Initialize features that need to hook events immediately
        this.serviceProvider.GetRequiredService<Marketeer.Features.CharacterTracking.Contracts.ICharacterTrackerService>();
        this.serviceProvider.GetRequiredService<Marketeer.Features.RetainerTracking.Contracts.IRetainerTrackerService>();

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
        // Adjust this if you changed it to trigger the Dashboard command instead
        var commandDispatcher = this.serviceProvider.GetService<CommandDispatcher>();
        // Fallback to manually opening the main window if needed
    }

    public void Dispose() {
        this.pluginInterface.UiBuilder.Draw -= this.windowSystem.Draw;
        this.pluginInterface.UiBuilder.OpenConfigUi -= this.OnOpenConfigUi;

        this.windowSystem.RemoveAllWindows();
        this.serviceProvider.Dispose();
    }
}