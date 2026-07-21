using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Marketeer.API.Configuration.Contracts;
using Marketeer.UI.Financials.UI;
using System.Numerics;

namespace Marketeer.UI.Configuration.UI;

public class ConfigWindow : Window {
    private IConfigurationService configurationService;
    private FinancialsMenu financialsTab;

    public ConfigWindow(IConfigurationService configurationService, FinancialsMenu financialsTab)
        : base("Marketeer Configuration", ImGuiWindowFlags.None) {
        this.configurationService = configurationService;
        this.financialsTab = financialsTab;

        this.SizeConstraints = new WindowSizeConstraints {
            MinimumSize = new Vector2(600, 400),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
    }

    public override void Draw() {
        if (ImGui.BeginTabBar("MarketeerTabs")) {

            if (ImGui.BeginTabItem("General")) {
                ImGui.TextUnformatted("General configuration settings will be added here.");
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }
    }
}