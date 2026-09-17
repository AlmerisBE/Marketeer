using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Marketeer.Core.Framework;
using Marketeer.UI.Configuration.UI;
using Marketeer.UI.Dashboard.UI;
using Marketeer.UI.Shell.Contracts;
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
        IContextMenu contextMenu) {

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

        this.pluginInterface.UiBuilder.Draw += this.windowSystem.Draw;
        this.pluginInterface.UiBuilder.OpenConfigUi += this.OnOpenConfigUi;
    }

    private void OnOpenConfigUi() {
        var navService = this.serviceProvider.GetService<INavigationService>();
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

        this.windowSystem.RemoveAllWindows();
        this.serviceProvider.Dispose();
    }
}