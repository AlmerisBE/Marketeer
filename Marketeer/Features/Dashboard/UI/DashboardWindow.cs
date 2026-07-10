using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Marketeer.Features.Dashboard.Contracts;
using Marketeer.Features.Localization.Contracts;
using System.Collections.Generic;
using System.Numerics;

namespace Marketeer.Features.Dashboard.UI;

public class DashboardWindow : Window {
    private IEnumerable<IDashboardWidget> widgets;
    private ILocalizationService localizationService;

    public DashboardWindow(IEnumerable<IDashboardWidget> widgets, ILocalizationService localizationService)
        : base(localizationService.Translate("Dashboard_Title"), ImGuiWindowFlags.None) {

        this.widgets = widgets;
        this.localizationService = localizationService;

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