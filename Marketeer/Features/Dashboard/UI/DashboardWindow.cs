using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Marketeer.Features.Dashboard.Contracts;
using Marketeer.Features.Localization.Contracts;
using Marketeer.Features.RetainerAutomation.Contracts;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Marketeer.Features.Dashboard.UI;

public class DashboardWindow : Window {
    private IReadOnlyList<INavigationNode> navigationNodes;
    private ILocalizationService localizationService;
    private IRetainerAutomationService automationService;

    private INavigationNode? selectedNode;

    public DashboardWindow(
        IEnumerable<INavigationNode> navigationNodes,
        ILocalizationService localizationService,
        IRetainerAutomationService automationService)
        : base(localizationService.Translate("Dashboard_Title"), ImGuiWindowFlags.None) {

        this.navigationNodes = navigationNodes.OrderBy(node => node.Priority).ToList();
        this.selectedNode = this.navigationNodes.FirstOrDefault();

        this.localizationService = localizationService;
        this.automationService = automationService;

        this.SizeConstraints = new WindowSizeConstraints {
            MinimumSize = new Vector2(800, 500),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
    }

    public override void Draw() {
        // Left Sidebar (Tree Menu)
        if (ImGui.BeginChild("Sidebar", new Vector2(220, 0), true)) {

            // Reserve 30 pixels at the bottom for the scan button
            if (ImGui.BeginChild("TreeArea", new Vector2(0, -30), false)) {
                this.DrawNodeTree(this.navigationNodes);
                ImGui.EndChild();
            }

            // Draw the scan button at the very bottom, taking full width
            if (ImGui.Button("Scan Retainers", new Vector2(-1, 24f))) {
                this.automationService.TriggerScan();
            }

            ImGui.EndChild();
        }

        ImGui.SameLine();

        // Right Main Content Area
        if (ImGui.BeginChild("MainContent", new Vector2(0, 0), false)) {

            if (this.selectedNode != null && this.selectedNode.HasContent) {
                this.selectedNode.DrawContent();
            }

            ImGui.EndChild();
        }
    }

    private void DrawNodeTree(IEnumerable<INavigationNode> nodes) {
        foreach (var node in nodes) {
            var children = node.GetChildren().ToList();
            bool isLeaf = children.Count == 0;

            var flags = ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.SpanAvailWidth;
            if (isLeaf) {
                flags |= ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen;
            }
            if (node.DefaultExpanded) {
                flags |= ImGuiTreeNodeFlags.DefaultOpen;
            }
            if (this.selectedNode == node) {
                flags |= ImGuiTreeNodeFlags.Selected;
            }

            bool isOpen = ImGui.TreeNodeEx(node.Name, flags);

            if (ImGui.IsItemClicked() && !ImGui.IsItemToggledOpen()) {
                this.selectedNode = node;
            }

            if (isOpen && !isLeaf) {
                this.DrawNodeTree(children);
                ImGui.TreePop();
            }
        }
    }
}