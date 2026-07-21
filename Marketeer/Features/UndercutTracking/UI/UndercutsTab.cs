using Dalamud.Bindings.ImGui;
using Marketeer.Features.Dashboard.Contracts;
using Marketeer.Features.Localization.Contracts;

namespace Marketeer.Features.UndercutTracking.UI;

public class UndercutsTab : IDashboardTab {
    private ILocalizationService localizationService;

    public string Name => this.localizationService.Translate("Undercuts_TabName");

    // Positioned after Financials (Priority 30)
    public int Priority => 40;

    public UndercutsTab(ILocalizationService localizationService) {
        this.localizationService = localizationService;
    }

    public void Draw() {
        var message = this.localizationService.Translate("Undercuts_WorkInProgress");

        ImGui.Spacing();
        ImGui.TextDisabled(message);
    }
}