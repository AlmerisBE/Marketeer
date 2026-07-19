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
    private IReadOnlyList<IDashboardTab> tabs;
    private ILocalizationService localizationService;
    private IRetainerAutomationService automationService;

    public DashboardWindow(
        IEnumerable<IDashboardTab> tabs,
        ILocalizationService localizationService,
        IRetainerAutomationService automationService)
        : base(localizationService.Translate("Dashboard_Title"), ImGuiWindowFlags.None) {

        this.tabs = tabs.OrderBy(tab => tab.Priority).ToList();

        this.localizationService = localizationService;
        this.automationService = automationService;

        this.SizeConstraints = new WindowSizeConstraints {
            MinimumSize = new Vector2(500, 350),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
    }

    public override unsafe void Draw() {
        float availableWidth = ImGui.GetWindowContentRegionMax().X;
        float buttonWidth = 110f;

        ImGui.SetCursorPosX(availableWidth - buttonWidth);
        if (ImGui.Button("Scan Retainers", new Vector2(buttonWidth, 24f))) {
            this.automationService.TriggerScan();
        }

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 28f);

        if (ImGui.BeginTabBar("MarketeerDashboardTabs", ImGuiTabBarFlags.None)) {
            foreach (var tab in this.tabs) {
                if (ImGui.BeginTabItem(tab.Name, ImGuiTabItemFlags.NoReorder)) {
                    tab.Draw();
                    ImGui.EndTabItem();
                }
            }

            ImGui.EndTabBar();
        }
    }
}