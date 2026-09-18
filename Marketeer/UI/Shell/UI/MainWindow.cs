using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Marketeer.UI.Localization.Contracts;
using Marketeer.UI.Shell.Contracts;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Marketeer.UI.Shell.UI;

public class MainWindow : Window {
    private IReadOnlyList<INavigationNode> rootNodes;
    private IReadOnlyList<ISidebarAction> sidebarActions;
    private ILocalizationService localizationService;
    private INavigationService navigationService;

    public MainWindow(
        IEnumerable<INavigationNode> navigationNodes,
        IEnumerable<ISidebarAction> sidebarActions,
        ILocalizationService localizationService,
        INavigationService navigationService)
        : base(localizationService.Translate("Dashboard_Title"), ImGuiWindowFlags.None) {

        this.rootNodes = navigationNodes.OrderBy(node => node.Priority).ToList();
        this.sidebarActions = sidebarActions.OrderBy(a => a.Priority).ToList();
        this.localizationService = localizationService;
        this.navigationService = navigationService;

        if (this.navigationService.SelectedNode == null) {
            var defaultNode = this.rootNodes.FirstOrDefault();
            if (defaultNode != null) this.navigationService.NavigateTo(defaultNode);
        }

        this.SizeConstraints = new WindowSizeConstraints {
            MinimumSize = new Vector2(800, 500),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
    }

    public override void Draw() {
        if (ImGui.BeginChild("Sidebar", new Vector2(220, 0), true)) {
            if (ImGui.BeginChild("TreeArea", new Vector2(0, -30), false)) {
                var groupedNodes = this.rootNodes
                    .GroupBy(n => n.GroupName)
                    .OrderBy(g => g.Min(n => n.Priority));

                foreach (var group in groupedNodes) {
                    if (string.IsNullOrEmpty(group.Key)) this.DrawNodeTree(group);
                    else if (ImGui.CollapsingHeader(group.Key, ImGuiTreeNodeFlags.DefaultOpen)) {
                        ImGui.Indent(10f);
                        this.DrawNodeTree(group);
                        ImGui.Unindent(10f);
                    }
                }
                ImGui.EndChild();
            }

            foreach (var action in this.sidebarActions) {
                if (ImGui.Button(action.Name, new Vector2(-1, 24f))) action.Execute();
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
            if (isLeaf) flags |= ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen;
            if (node.DefaultExpanded) flags |= ImGuiTreeNodeFlags.DefaultOpen;
            if (this.navigationService.SelectedNode == node) flags |= ImGuiTreeNodeFlags.Selected;

            bool isOpen = ImGui.TreeNodeEx(node.Name, flags);

            if (ImGui.IsItemClicked() && !ImGui.IsItemToggledOpen()) this.navigationService.NavigateTo(node);

            if (isOpen && !isLeaf) {
                this.DrawNodeTree(children);
                ImGui.TreePop();
            }
        }
    }
}