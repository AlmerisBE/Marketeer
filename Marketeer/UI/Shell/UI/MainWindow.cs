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
        : base($"Marketeer v{pluginInterface.Manifest?.AssemblyVersion?.ToString() ?? "Dev"}", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse) {

        this.navigationService = navigationService;
        this.nodes = nodes.OrderBy(n => n.Priority).ToList();
        this.toolbarActions = toolbarActions.OrderBy(a => a.Priority).ToList();
        this.statusBarProviders = statusBarProviders.OrderBy(p => p.Priority).ToList();

        this.Size = new Vector2(900f, 650f);
        this.SizeCondition = ImGuiCond.FirstUseEver;

        this.SizeConstraints = new WindowSizeConstraints {
            MinimumSize = new Vector2(800f, 500f),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        this.ShowCloseButton = true;
        this.AllowPinning = true;
        this.AllowClickthrough = true;
        this.RespectCloseHotkey = true;

        if (this.navigationService.CurrentNode == null && this.nodes.Count > 0) this.navigationService.NavigateTo(this.nodes[0]);
    }

    public override void Draw() {
        this.DrawToolbar();
        ImGui.Separator();

        // Calculate the footer height including spacing, similar to the reference layout implementation.
        float footerHeight = ImGui.GetFrameHeight() + (ImGui.GetStyle().ItemSpacing.Y * 2f);

        // Wrap the entire main content area in a BeginChild with a negative Y dimension 
        // to robustly reserve space for the footer without clipping it.
        if (ImGui.BeginChild("MainContent", new Vector2(0f, -footerHeight), false, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)) {
            // The table can now safely consume all available space within this managed child wrapper.
            if (ImGui.BeginTable("MainLayout", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.Resizable)) {
                ImGui.TableSetupColumn("Sidebar", ImGuiTableColumnFlags.WidthFixed, 200f);
                ImGui.TableSetupColumn("Content", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableNextRow();

                ImGui.TableNextColumn();
                if (ImGui.BeginChild("SidebarScrollArea", new Vector2(0f, 0f), false)) {
                    this.DrawSidebar();
                    ImGui.EndChild();
                }

                ImGui.TableNextColumn();
                if (ImGui.BeginChild("ContentScrollArea", new Vector2(0f, 0f), false)) {
                    if (this.navigationService.CurrentNode != null) this.navigationService.CurrentNode.DrawContent();
                    ImGui.EndChild();
                }

                ImGui.EndTable();
            }
        }
        ImGui.EndChild();

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
        var height = ImGui.GetFrameHeight();
        if (ImGui.BeginChild("Toolbar", new Vector2(0f, height), false, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)) {
            bool isFirst = true;
            foreach (var action in this.toolbarActions) {
                if (!isFirst) ImGui.SameLine();

                if (!action.IsEnabled) ImGui.BeginDisabled();
                if (ImGui.Button(action.Name)) action.Execute();
                if (!action.IsEnabled) ImGui.EndDisabled();

                if (ImGui.IsItemHovered()) ImGui.SetTooltip(action.Tooltip);

                isFirst = false;
            }
        }
        ImGui.EndChild();
    }

    private void DrawStatusBar() {
        var height = ImGui.GetFrameHeight();

        if (ImGui.BeginChild("StatusBar", new Vector2(0f, height), false, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)) {
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
        }
        ImGui.EndChild();
    }
}