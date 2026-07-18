using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Marketeer.Features.Dashboard.Contracts;
using Marketeer.Features.Financials.UI;
using Marketeer.Features.Localization.Contracts;
using Marketeer.Features.RetainerAutomation.Contracts;
using System.Collections.Generic;
using System.Numerics;

namespace Marketeer.Features.Dashboard.UI;

public class DashboardWindow : Window {
    private IEnumerable<IDashboardWidget> widgets;
    private ILocalizationService localizationService;
    private FinancialsTab financialsTab;
    private IRetainerAutomationService automationService;

    public DashboardWindow(
        IEnumerable<IDashboardWidget> widgets,
        ILocalizationService localizationService,
        FinancialsTab financialsTab,
        IRetainerAutomationService automationService)
        : base(localizationService.Translate("Dashboard_Title"), ImGuiWindowFlags.None) {
        this.widgets = widgets;
        this.localizationService = localizationService;
        this.financialsTab = financialsTab;
        this.automationService = automationService;

        this.SizeConstraints = new WindowSizeConstraints {
            MinimumSize = new Vector2(500, 350),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
    }

    public override void Draw() {
        // Right-aligned button positioning beside the layout tab bar
        float availableWidth = ImGui.GetWindowContentRegionMax().X;
        float buttonWidth = 110f;

        ImGui.SetCursorPosX(availableWidth - buttonWidth);
        if (ImGui.Button("Scan Retainers", new Vector2(buttonWidth, 24f))) {
            this.automationService.TriggerScan();
        }

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 28f); // Align context line back to tab row bounds

        if (ImGui.BeginTabBar("DashboardTabs")) {
            foreach (var widget in this.widgets) {
                if (ImGui.BeginTabItem(widget.Name)) {
                    widget.Draw();
                    ImGui.EndTabItem();
                }
            }

            if (ImGui.BeginTabItem(this.localizationService.Translate("Financials_TabName") ?? "Financials")) {
                this.financialsTab.Draw();
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }
    }
}