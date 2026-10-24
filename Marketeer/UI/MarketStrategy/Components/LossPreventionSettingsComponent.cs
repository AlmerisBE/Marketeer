using Dalamud.Bindings.ImGui;
using Marketeer.Core.Configuration.Models;
using Marketeer.Core.MarketStrategy.Models;
using Marketeer.UI.Localization.Contracts;
using System;

namespace Marketeer.UI.MarketStrategy.Components;

public class LossPreventionSettingsComponent {
    private ILocalizationService localizationService;

    public LossPreventionSettingsComponent(ILocalizationService localizationService) {
        this.localizationService = localizationService;
    }

    public bool Draw(PluginConfiguration config) {
        bool changed = false;

        bool enforceVendor = config.EnforceVendorPriceMinimum;
        if (ImGui.Checkbox(this.localizationService.Translate("Config_EnforceVendorPriceMinimum") ?? "Enforce Vendor Minimum", ref enforceVendor)) {
            config.EnforceVendorPriceMinimum = enforceVendor;
            changed = true;
        }
        if (ImGui.IsItemHovered()) ImGui.SetTooltip(this.localizationService.Translate("Config_EnforceVendorPriceMinimum_Tooltip"));

        if (!config.EnforceVendorPriceMinimum) ImGui.BeginDisabled();

        ImGui.Spacing();

        ImGui.TextUnformatted(this.localizationService.Translate("Config_LossBehavior") ?? "Action if Target < Vendor Price");
        ImGui.SetNextItemWidth(250f);
        if (ImGui.BeginCombo("##LossBehavior", this.localizationService.Translate($"Config_LossBehavior_{config.LossBehavior}"))) {
            foreach (MinimumPriceBehavior behavior in Enum.GetValues(typeof(MinimumPriceBehavior))) {
                if (ImGui.Selectable(this.localizationService.Translate($"Config_LossBehavior_{behavior}"), config.LossBehavior == behavior)) {
                    config.LossBehavior = behavior;
                    changed = true;
                }
            }
            ImGui.EndCombo();
        }
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) ImGui.SetTooltip(this.localizationService.Translate("Config_LossBehavior_Tooltip"));

        ImGui.Spacing();

        ImGui.TextUnformatted(this.localizationService.Translate("Config_CancelPriority_Label") ?? "Cancellation Priority");
        ImGui.SetNextItemWidth(250f);
        if (ImGui.BeginCombo("##CancelPriority", this.localizationService.Translate($"Config_CancelPriority_{config.CancelInventoryPriority}"))) {
            foreach (InventoryPriority priority in Enum.GetValues(typeof(InventoryPriority))) {
                if (ImGui.Selectable(this.localizationService.Translate($"Config_CancelPriority_{priority}"), config.CancelInventoryPriority == priority)) {
                    config.CancelInventoryPriority = priority;
                    changed = true;
                }
            }
            ImGui.EndCombo();
        }
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) ImGui.SetTooltip(this.localizationService.Translate("Config_CancelPriority_Tooltip"));

        if (!config.EnforceVendorPriceMinimum) ImGui.EndDisabled();

        return changed;
    }
}