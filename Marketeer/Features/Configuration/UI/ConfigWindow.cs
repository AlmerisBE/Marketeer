using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Marketeer.Features.Configuration.Contracts;
using Marketeer.Features.Financials.UI;
using System.Numerics;

namespace Marketeer.Features.Configuration.UI;

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
                var config = this.configurationService.GetConfig();
                var exampleValue = config.ExampleCheckbox;

                if (ImGui.Checkbox("Example Checkbox", ref exampleValue)) {
                    config.ExampleCheckbox = exampleValue;
                    this.configurationService.Save();
                }
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }
    }
}