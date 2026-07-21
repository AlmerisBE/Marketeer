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
    private IReadOnlyList<INavigationNode> rootNodes;
    private ILocalizationService localizationService;
    private IRetainerAutomationService automationService;
    private IDashboardNavigationService navigationService;

    public DashboardWindow(
        IEnumerable<INavigationNode> navigationNodes,
        ILocalizationService localizationService,
        IRetainerAutomationService automationService,
        IDashboardNavigationService navigationService)
        : base(localizationService.Translate("Dashboard_Title"), ImGuiWindowFlags.None) {

        this.rootNodes = navigationNodes.OrderBy(node => node.Priority).ToList();
        this.localizationService = localizationService;
        this.automationService = automationService;
        this.navigationService = navigationService;

        // Default home page selection, checking for null to avoid CS8604
        if (this.navigationService.SelectedNode == null) {
            var defaultNode = this.rootNodes.FirstOrDefault();
            if (defaultNode != null) {
                this.navigationService.NavigateTo(defaultNode);
            }
        }

        this.SizeConstraints = new WindowSizeConstraints {
            MinimumSize = new Vector2(800, 500),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
    }

    public override void Draw() {
        if (ImGui.BeginChild("Sidebar", new Vector2(220, 0), true)) {

            if (ImGui.BeginChild("TreeArea", new Vector2(0, -30), false)) {
                this.DrawNodeTree(this.rootNodes);
                ImGui.EndChild();
            }

            if (ImGui.Button("Scan Retainers", new Vector2(-1, 24f))) {
                this.automationService.TriggerScan();
            }

            ImGui.EndChild();
        }

        ImGui.SameLine();

        if (ImGui.BeginChild("MainContent", new Vector2(0, 0), false)) {
            if (this.navigationService.SelectedNode != null && this.navigationService.SelectedNode.HasContent) {
                this.navigationService.SelectedNode.DrawContent();
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
            if (this.navigationService.SelectedNode == node) {
                flags |= ImGuiTreeNodeFlags.Selected;
            }

            bool isOpen = ImGui.TreeNodeEx(node.Name, flags);

            if (ImGui.IsItemClicked() && !ImGui.IsItemToggledOpen()) {
                this.navigationService.NavigateTo(node);
            }

            if (isOpen && !isLeaf) {
                this.DrawNodeTree(children);
                ImGui.TreePop();
            }
        }
    }
}