using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Marketeer.API.Guidance.Contracts;
using Marketeer.API.Guidance.Models;
using Marketeer.API.Localization.Contracts;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Marketeer.UI.Guidance.UI;

public class MarketeerGuideWindow : Window {
    private IWindowGeometryProvider geometryProvider;
    private IEnumerable<IGuidanceInstructionProvider> instructionProviders;
    private ILocalizationService localization;

    public MarketeerGuideWindow(IWindowGeometryProvider geometryProvider, IEnumerable<IGuidanceInstructionProvider> instructionProviders, ILocalizationService localization)
        : base("Marketeer Guide", ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoFocusOnAppearing) {

        this.geometryProvider = geometryProvider;
        this.instructionProviders = instructionProviders;
        this.localization = localization;
        this.IsOpen = true;
    }

    public override bool DrawConditions() {
        return this.geometryProvider.GetWindowGeometry("RetainerSellList", out _, out _, out _, out _, out _);
    }

    public override void PreDraw() {
        if (this.geometryProvider.GetWindowGeometry("RetainerSellList", out var x, out var y, out _, out _, out _)) {
            ImGui.SetNextWindowPos(new Vector2(x, y), ImGuiCond.Always, new Vector2(1.0f, 0.0f));
        }
    }

    public override void Draw() {
        ImGui.TextColored(new Vector4(0.2f, 0.8f, 0.2f, 1.0f), "Marketeer Guide");
        ImGui.Separator();
        ImGui.Spacing();

        var instruction = this.instructionProviders.Select(p => p.GetCurrentInstruction()).FirstOrDefault(i => i != null);

        if (instruction == null) {
            ImGui.TextUnformatted(this.localization.Translate("Guidance_Waiting"));
            return;
        }

        string title = instruction.ActionType == GuidanceActionType.UpdatePrice ? this.localization.Translate("Guidance_Action_UpdatePrice") : this.localization.Translate("Guidance_Action_CancelListing");

        ImGui.TextColored(new Vector4(1.0f, 0.8f, 0.2f, 1.0f), title);
        ImGui.TextUnformatted(this.localization.Translate("Guidance_ItemName", instruction.ItemName));

        if (instruction.ActionType == GuidanceActionType.UpdatePrice && instruction.TargetPrice.HasValue) {
            ImGui.TextUnformatted(this.localization.Translate("Guidance_TargetPrice", instruction.TargetPrice.Value.ToString("N0")));
        }
    }
}