using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Marketeer.Features.Dashboard.Contracts;
using Marketeer.Features.Financials.UI;
using Marketeer.Features.Localization.Contracts;
using System.Collections.Generic;
using System.Numerics;

namespace Marketeer.Features.Dashboard.UI;

public class DashboardWindow : Window {
    private IEnumerable<IDashboardWidget> widgets;
    private ILocalizationService localizationService;
    private FinancialsTab financialsTab;

    public DashboardWindow(IEnumerable<IDashboardWidget> widgets, ILocalizationService localizationService, FinancialsTab financialsTab)
        : base(localizationService.Translate("Dashboard_Title"), ImGuiWindowFlags.None) {
        this.widgets = widgets;
        this.localizationService = localizationService;
        this.financialsTab = financialsTab;

        this.SizeConstraints = new WindowSizeConstraints {
            MinimumSize = new Vector2(500, 350),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
    }

    public override void Draw() {
        if (ImGui.BeginTabBar("DashboardTabs")) {
            // Render existing tracked character list widgets
            foreach (var widget in this.widgets) {
                if (ImGui.BeginTabItem(widget.Name)) {
                    widget.Draw();
                    ImGui.EndTabItem();
                }
            }

            // Render the integrated global financial summary tab directly into the dashboard
            if (ImGui.BeginTabItem(this.localizationService.Translate("Financials_TabName") ?? "Financials")) {
                this.financialsTab.Draw();
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }
    }
}