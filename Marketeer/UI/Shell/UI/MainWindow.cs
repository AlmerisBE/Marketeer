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
    private IReadOnlyList<INavigationNode> nodes;
    private IReadOnlyList<IToolbarAction> toolbarActions;
    private IReadOnlyList<IStatusBarProvider> statusBarProviders;

    public MainWindow(
        IDalamudPluginInterface pluginInterface,
        INavigationService navigationService,
        IEnumerable<INavigationNode> nodes,
        IEnumerable<IToolbarAction> toolbarActions,
        IEnumerable<IStatusBarProvider> statusBarProviders)
        : base($"Marketeer v{pluginInterface.Manifest?.AssemblyVersion?.ToString() ?? "Dev"}###MarketeerMainWindow", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse) {

        this.navigationService = navigationService;
        this.nodes = nodes.OrderBy(n => n.Priority).ToList();
        this.toolbarActions = toolbarActions.OrderBy(a => a.Priority).ToList();
        this.statusBarProviders = statusBarProviders.OrderBy(p => p.Priority).ToList();

        this.Size = new Vector2(900, 650);
        this.SizeCondition = ImGuiCond.FirstUseEver;

        this.SizeConstraints = new WindowSizeConstraints {
            MinimumSize = new Vector2(800, 500),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        if (this.navigationService.CurrentNode == null && this.nodes.Count > 0) this.navigationService.NavigateTo(this.nodes[0]);
    }

    public override void Draw() {
        this.DrawToolbar();
        ImGui.Separator();

        // Calculate available height while leaving room for the bottom status bar and separator
        var tableHeight = ImGui.GetContentRegionAvail().Y - 30f;

        // Apply height constraint directly to the table
        if (ImGui.BeginTable("MainLayout", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.Resizable, new Vector2(0, tableHeight))) {
            ImGui.TableSetupColumn("Sidebar", ImGuiTableColumnFlags.WidthFixed, 200f);
            ImGui.TableSetupColumn("Content", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            if (ImGui.BeginChild("SidebarScrollArea", new Vector2(0, 0), false)) {
                this.DrawSidebar();
                ImGui.EndChild();
            }

            ImGui.TableNextColumn();
            if (ImGui.BeginChild("ContentScrollArea", new Vector2(0, 0), false)) {
                if (this.navigationService.CurrentNode != null) this.navigationService.CurrentNode.DrawContent();
                ImGui.EndChild();
            }

            ImGui.EndTable();
        }

        ImGui.Separator();
        this.DrawStatusBar();
    }

    private void DrawSidebar() {
        var groupedNodes = this.nodes.GroupBy(n => n.GroupName);

        foreach (var group in groupedNodes) {
            if (string.IsNullOrEmpty(group.Key)) {
                foreach (var node in group) {
                    this.DrawNodeTree(node);
                }
            }
            else {
                bool isGroupActive = group.Any(n => this.IsNodeActiveOrContainsActive(n));
                var flags = ImGuiTreeNodeFlags.CollapsingHeader;

                if (isGroupActive) flags |= ImGuiTreeNodeFlags.DefaultOpen;

                if (ImGui.TreeNodeEx(group.Key, flags)) {
                    foreach (var node in group) {
                        this.DrawNodeTree(node);
                    }
                }
            }
        }
    }

    private bool IsNodeActiveOrContainsActive(INavigationNode node) {
        if (node == this.navigationService.CurrentNode) return true;

        foreach (var child in node.GetChildren()) {
            if (this.IsNodeActiveOrContainsActive(child)) return true;
        }

        return false;
    }

    private void DrawNodeTree(INavigationNode node) {
        var children = node.GetChildren().ToList();
        bool hasChildren = children.Count > 0;

        if (hasChildren) {
            var flags = ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.OpenOnDoubleClick;
            bool isActiveHierarchy = this.IsNodeActiveOrContainsActive(node);

            if (node.DefaultExpanded || isActiveHierarchy) flags |= ImGuiTreeNodeFlags.DefaultOpen;
            if (this.navigationService.CurrentNode == node) flags |= ImGuiTreeNodeFlags.Selected;

            bool isOpen = ImGui.TreeNodeEx(node.Name, flags);

            if (ImGui.IsItemClicked() && !ImGui.IsItemToggledOpen()) this.navigationService.NavigateTo(node);

            if (isOpen) {
                foreach (var child in children) {
                    this.DrawNodeTree(child);
                }
                ImGui.TreePop();
            }
        }
        else {
            bool isSelected = this.navigationService.CurrentNode == node;
            if (ImGui.Selectable(node.Name, isSelected)) this.navigationService.NavigateTo(node);
        }
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