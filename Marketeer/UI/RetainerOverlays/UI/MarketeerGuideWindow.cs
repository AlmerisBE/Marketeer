using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using Marketeer.API.GameInterop.Contracts;
using Marketeer.Core.CompetitionTracking.Contracts;
using Marketeer.Core.MarketListings.Contracts;
using Marketeer.Core.RetainerAutomation.Contracts;
using Marketeer.UI.Localization.Contracts;
using Marketeer.UI.RetainerOverlays.Contracts;
using Marketeer.UI.RetainerOverlays.Models;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Marketeer.UI.RetainerOverlays.UI;

public class MarketeerGuideWindow : Window {
    private readonly IWindowGeometryProvider geometryProvider;
    private readonly IEnumerable<IGuidanceInstructionProvider> instructionProviders;
    private readonly ILocalizationService localization;
    private readonly IRetainerSwitcherService switcherService;
    private readonly ICompetitionStateService competitionState;
    private readonly IListingOptimizationService optimizationService;
    private readonly IObjectTable objectTable;
    private readonly IMarketListingProvider listingProvider;
    private readonly IRetainerProvider retainerProvider;
    private readonly IRetainerUiInteractionService uiInteraction;

    public MarketeerGuideWindow(
        IWindowGeometryProvider geometryProvider,
        IEnumerable<IGuidanceInstructionProvider> instructionProviders,
        ILocalizationService localization,
        IRetainerSwitcherService switcherService,
        ICompetitionStateService competitionState,
        IListingOptimizationService optimizationService,
        IObjectTable objectTable,
        IMarketListingProvider listingProvider,
        IRetainerProvider retainerProvider,
        IRetainerUiInteractionService uiInteraction)
        : base("Marketeer Guide", ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoFocusOnAppearing) {

        this.geometryProvider = geometryProvider;
        this.instructionProviders = instructionProviders;
        this.localization = localization;
        this.switcherService = switcherService;
        this.competitionState = competitionState;
        this.optimizationService = optimizationService;
        this.objectTable = objectTable;
        this.listingProvider = listingProvider;
        this.retainerProvider = retainerProvider;
        this.uiInteraction = uiInteraction;
        this.IsOpen = true;
    }

    public override bool DrawConditions() {
        if (this.geometryProvider.GetWindowGeometry("RetainerSellList", out _, out _, out _, out _, out _)) {
            return true;
        }

        if (this.geometryProvider.GetWindowGeometry("RetainerList", out _, out _, out _, out _, out _)) {
            return true;
        }

        if (this.geometryProvider.GetWindowGeometry("SelectString", out _, out _, out _, out _, out _)) {
            var activeId = this.listingProvider.GetActiveRetainerId();
            if (activeId.HasValue) {
                var activeRetainer = this.retainerProvider.GetActiveRetainers().FirstOrDefault(r => r.RetainerId == activeId.Value);
                if (activeRetainer != null && this.uiInteraction.IsMenuReadyForRetainer(activeRetainer.Name)) {
                    return true;
                }
            }
        }

        return false;
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
            ImGui.TextUnformatted(this.localization.Translate("Guidance_ItemName", $"{instruction.ItemName} x{instruction.Quantity}"));
            if (instruction.ActionType == GuidanceActionType.UpdatePrice && instruction.TargetPrice.HasValue) {
                ImGui.TextUnformatted(this.localization.Translate("Guidance_TargetPrice", instruction.TargetPrice.Value.ToString("N0")));
            }
        }

        if (instruction.ActionType == GuidanceActionType.SummonRetainer || instruction.ActionType == GuidanceActionType.SwitchRetainer) {
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            bool hasMarketActions = false;
            var player = this.objectTable.LocalPlayer;

            if (player != null) {
                var playerName = player.Name.TextValue;
                hasMarketActions = this.competitionState.GetUndercutItems().Any(u => u.CharacterName == playerName && u.RetainerName == instruction.RetainerName) ||
                                   this.optimizationService.GetVendorPricedListings().Any(s => s.CharacterName == playerName && s.RetainerName == instruction.RetainerName);
            }

            if (ImGui.Button(this.localization.Translate("Guidance_Btn_SwitchAuto", instruction.RetainerName), new Vector2(-1, 0))) {
                this.switcherService.SwitchTo(instruction.RetainerName, hasMarketActions);
            }
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.TextDisabled(this.localization.Translate("Guidance_Legend_Title"));
        ImGui.TextColored(new Vector4(1.0f, 0.23f, 0.23f, 1.0f), this.localization.Translate("Guidance_Legend_Red"));
        ImGui.TextColored(new Vector4(0.9f, 0.9f, 0.35f, 1.0f), this.localization.Translate("Guidance_Legend_Yellow"));
        ImGui.TextColored(new Vector4(1.0f, 0.59f, 0.2f, 1.0f), this.localization.Translate("Guidance_Legend_Orange"));
    }
}