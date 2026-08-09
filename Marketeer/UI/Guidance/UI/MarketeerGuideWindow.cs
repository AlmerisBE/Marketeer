using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Marketeer.API.Guidance.Contracts;
using Marketeer.API.Guidance.Models;
using Marketeer.API.Localization.Contracts;
using Marketeer.API.RetainerAutomation.Contracts;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Marketeer.UI.Guidance.UI;

public class MarketeerGuideWindow : Window {
    private IWindowGeometryProvider geometryProvider;
    private IEnumerable<IGuidanceInstructionProvider> instructionProviders;
    private ILocalizationService localization;
    private IRetainerSwitcherService switcherService;

    public MarketeerGuideWindow(
        IWindowGeometryProvider geometryProvider,
        IEnumerable<IGuidanceInstructionProvider> instructionProviders,
        ILocalizationService localization,
        IRetainerSwitcherService switcherService)
        : base("Marketeer Guide", ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoFocusOnAppearing) {

        this.geometryProvider = geometryProvider;
        this.instructionProviders = instructionProviders;
        this.localization = localization;
        this.switcherService = switcherService;
        this.IsOpen = true;
    }

    public override bool DrawConditions() {
        return this.geometryProvider.GetWindowGeometry("RetainerSellList", out _, out _, out _, out _, out _) ||
               this.geometryProvider.GetWindowGeometry("SelectString", out _, out _, out _, out _, out _) ||
               this.geometryProvider.GetWindowGeometry("RetainerList", out _, out _, out _, out _, out _);
    }

    public override void PreDraw() {
        if (this.geometryProvider.GetWindowGeometry("RetainerSellList", out var x, out var y, out _, out _, out _)) {
            ImGui.SetNextWindowPos(new Vector2(x, y), ImGuiCond.Always, new Vector2(1.0f, 0.0f));
        }
        else if (this.geometryProvider.GetWindowGeometry("SelectString", out x, out y, out _, out _, out _)) {
            ImGui.SetNextWindowPos(new Vector2(x, y), ImGuiCond.Always, new Vector2(1.0f, 0.0f));
        }
        else if (this.geometryProvider.GetWindowGeometry("RetainerList", out x, out y, out _, out _, out _)) {
            ImGui.SetNextWindowPos(new Vector2(x, y), ImGuiCond.Always, new Vector2(1.0f, 0.0f));
        }
    }

    public override void Draw() {
        ImGui.Dummy(new Vector2(220f, 0f));

        var instruction = this.instructionProviders.Select(p => p.GetCurrentInstruction()).FirstOrDefault(i => i != null);

        if (instruction == null) {
            ImGui.TextColored(new Vector4(0.2f, 1.0f, 0.2f, 1.0f), this.localization.Translate("Guidance_AllClear_Title"));
            ImGui.TextUnformatted(this.localization.Translate("Guidance_AllClear_Desc"));
            return;
        }

        string title = instruction.ActionType switch {
            GuidanceActionType.UpdatePrice => this.localization.Translate("Guidance_Action_UpdatePrice"),
            GuidanceActionType.CancelListing => this.localization.Translate("Guidance_Action_CancelListing"),
            GuidanceActionType.SwitchRetainer => this.localization.Translate("Guidance_Action_SwitchRetainer"),
            GuidanceActionType.SummonRetainer => this.localization.Translate("Guidance_Action_SummonRetainer"),
            GuidanceActionType.SwitchCharacter => this.localization.Translate("Guidance_Action_SwitchCharacter"),
            _ => "Guidance"
        };

        ImGui.TextColored(new Vector4(1.0f, 0.8f, 0.2f, 1.0f), title);

        if (instruction.ActionType == GuidanceActionType.SwitchCharacter) {
            ImGui.TextUnformatted(this.localization.Translate("Guidance_LogInAs", instruction.CharacterName));
        }
        else if (instruction.ActionType == GuidanceActionType.SwitchRetainer) {
            ImGui.TextUnformatted(this.localization.Translate("Guidance_SwitchTo", instruction.RetainerName));
        }
        else if (instruction.ActionType == GuidanceActionType.SummonRetainer) {
            ImGui.TextUnformatted(this.localization.Translate("Guidance_Summon", instruction.RetainerName));
        }
        else {
            ImGui.TextUnformatted(this.localization.Translate("Guidance_ItemName", instruction.ItemName));
            if (instruction.ActionType == GuidanceActionType.UpdatePrice && instruction.TargetPrice.HasValue) {
                ImGui.TextUnformatted(this.localization.Translate("Guidance_TargetPrice", instruction.TargetPrice.Value.ToString("N0")));
            }
        }

        // --- Unified Automation Button ---
        if (instruction.ActionType == GuidanceActionType.SummonRetainer || instruction.ActionType == GuidanceActionType.SwitchRetainer) {
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            if (ImGui.Button(this.localization.Translate("Guidance_Btn_SwitchAuto", instruction.RetainerName), new Vector2(-1, 0))) {
                this.switcherService.SwitchTo(instruction.RetainerName);
            }
        }
    }
}