using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Marketeer.Features.Dashboard.Contracts;
using System.Collections.Generic;
using System.Numerics;

namespace Marketeer.Features.Dashboard.UI;

public class DashboardWindow : Window {
    private IEnumerable<IDashboardWidget> widgets;

    public DashboardWindow(IEnumerable<IDashboardWidget> widgets)
        : base("Marketeer - Dashboard", ImGuiWindowFlags.None) {

        this.widgets = widgets;

        this.SizeConstraints = new WindowSizeConstraints {
            MinimumSize = new Vector2(500, 350),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
    }

    public override void Draw() {
        if (ImGui.BeginTabBar("DashboardTabs")) {
            foreach (var widget in this.widgets) {
                if (ImGui.BeginTabItem(widget.Name)) {
                    widget.Draw();
                    ImGui.EndTabItem();
                }
            }
            ImGui.EndTabBar();
        }
    }
}