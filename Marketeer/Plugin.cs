using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Marketeer.Core.Framework;
using Marketeer.UI.Shell.UI;
using Marketeer.UI.Themes.Contracts;
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
        ITextureProvider textureProvider,
        IContextMenu contextMenu,
        IKeyState keyState) {

        this.pluginInterface = pluginInterface;
        this.windowSystem = new WindowSystem("Marketeer");

        var services = new ServiceCollection();

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
        services.AddSingleton(contextMenu);
        services.AddSingleton(keyState);

        services.AddPluginFeatures();

        this.serviceProvider = services.BuildServiceProvider();

        var features = this.serviceProvider.GetServices<IFeatureModule>();
        foreach (var feature in features) {
            feature.Initialize(this.serviceProvider);
        }

        var windows = this.serviceProvider.GetServices<Window>();
        foreach (var window in windows) {
            this.windowSystem.AddWindow(window);
        }

        this.pluginInterface.UiBuilder.Draw += this.OnDrawUi;
        this.pluginInterface.UiBuilder.OpenConfigUi += this.OnOpenConfigUi;
    }

    private void OnDrawUi() {
        var themeService = this.serviceProvider.GetService<IThemeService>();

        using var scope = themeService?.ApplyTheme();
        this.windowSystem.Draw();
    }

    private void OnOpenConfigUi() {
        var mainWindow = this.serviceProvider.GetService<MainWindow>();
        if (mainWindow != null) mainWindow.IsOpen = true;
    }

    public void Dispose() {
        this.pluginInterface.UiBuilder.Draw -= this.OnDrawUi;
        this.pluginInterface.UiBuilder.OpenConfigUi -= this.OnOpenConfigUi;

        this.windowSystem.RemoveAllWindows();
        this.serviceProvider.Dispose();
    }
}