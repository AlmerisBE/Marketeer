using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Marketeer.UI.Shell.Contracts;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Marketeer.UI.Shell.UI;

public class MainWindow : Window {
    private INavigationService navigationService;
    private IReadOnlyList<IToolbarAction> toolbarActions;
    private IReadOnlyList<IStatusBarProvider> statusBarProviders;

    public MainWindow(
        IDalamudPluginInterface pluginInterface,
        INavigationService navigationService,
        IEnumerable<IToolbarAction> toolbarActions,
        IEnumerable<IStatusBarProvider> statusBarProviders)
        : base($"Marketeer v{pluginInterface.Manifest?.AssemblyVersion?.ToString() ?? "Dev"}###MarketeerMainWindow", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse) {

        this.navigationService = navigationService;
        this.toolbarActions = toolbarActions.OrderBy(a => a.Priority).ToList();
        this.statusBarProviders = statusBarProviders.OrderBy(p => p.Priority).ToList();

        this.Size = new Vector2(900, 650);
        this.SizeCondition = ImGuiCond.FirstUseEver;
    }

    public override void Draw() {
        this.DrawToolbar();
        ImGui.Separator();

        var childHeight = ImGui.GetContentRegionAvail().Y - 30f;

        if (ImGui.BeginChild("MainContent", new Vector2(0, childHeight), false)) {
            if (this.navigationService.CurrentNode != null) this.navigationService.CurrentNode.DrawContent();
            ImGui.EndChild();
        }

        ImGui.Separator();
        this.DrawStatusBar();
    }

    private void DrawToolbar() {
        if (ImGui.BeginChild("Toolbar", new Vector2(0, 35f), false)) {
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 4f);

            bool isFirst = true;
            foreach (var action in this.toolbarActions) {
                if (!isFirst) ImGui.SameLine();

                if (!action.IsEnabled) ImGui.BeginDisabled();

                if (ImGui.Button(action.Name)) action.Execute();

                if (!action.IsEnabled) ImGui.EndDisabled();

                if (ImGui.IsItemHovered()) ImGui.SetTooltip(action.Tooltip);

                isFirst = false;
            }
            ImGui.EndChild();
        }
    }

    private void DrawStatusBar() {
        if (ImGui.BeginChild("StatusBar", new Vector2(0, 25f), false)) {
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 4f);

            bool isFirst = true;
            foreach (var provider in this.statusBarProviders) {
                if (!isFirst) {
                    ImGui.SameLine();
                    ImGui.TextDisabled("|");
                    ImGui.SameLine();
                }
                provider.Draw();
                isFirst = false;
            }
            ImGui.EndChild();
        }
    }
}