using Dalamud.Bindings.ImGui;
using Marketeer.Core.Configuration.Models;
using Marketeer.Core.MarketStrategy.Models;
using Marketeer.UI.Localization.Contracts;
using System;

namespace Marketeer.UI.MarketStrategy.Components;

public class UndercutSettingsComponent {
    private ILocalizationService localizationService;

    public UndercutSettingsComponent(ILocalizationService localizationService) {
        this.localizationService = localizationService;
    }

    public bool Draw(PluginConfiguration config) {
        bool changed = false;

        ImGui.TextUnformatted(this.localizationService.Translate("Config_UndercutMode") ?? "Undercut Mode");
        ImGui.SetNextItemWidth(250f);
        if (ImGui.BeginCombo("##UndercutMode", this.localizationService.Translate($"Config_UndercutMode_{config.UndercutMode}"))) {
            foreach (UndercutMode mode in Enum.GetValues(typeof(UndercutMode))) {
                if (ImGui.Selectable(this.localizationService.Translate($"Config_UndercutMode_{mode}"), config.UndercutMode == mode)) {
                    config.UndercutMode = mode;
                    changed = true;
                }
            }
            ImGui.EndCombo();
        }
        if (ImGui.IsItemHovered()) ImGui.SetTooltip(this.localizationService.Translate("Config_UndercutMode_Tooltip"));

        ImGui.Spacing();

        if (config.UndercutMode == UndercutMode.Absolute) {
            ImGui.TextUnformatted(this.localizationService.Translate("Config_UndercutAmount") ?? "Amount");
            ImGui.SetNextItemWidth(150f);
            int amount = (int)config.UndercutAmount;
            if (ImGui.InputInt("##UndercutAmount", ref amount)) {
                if (amount < 0) amount = 0;
                config.UndercutAmount = (uint)amount;
                changed = true;
            }
            if (ImGui.IsItemHovered()) ImGui.SetTooltip(this.localizationService.Translate("Config_UndercutAmount_Tooltip"));
        }
        else {
            ImGui.TextUnformatted(this.localizationService.Translate("Config_UndercutPercentage") ?? "Percentage");
            ImGui.SetNextItemWidth(250f);
            float percentage = (float)config.UndercutRelativePercentage;
            if (ImGui.SliderFloat("##UndercutPercentage", ref percentage, 0.1f, 100.0f, "%.1f%%")) {
                config.UndercutRelativePercentage = Math.Round(percentage, 1);
                changed = true;
            }
            if (ImGui.IsItemHovered()) ImGui.SetTooltip(this.localizationService.Translate("Config_UndercutPercentage_Tooltip"));
        }

        return changed;
    }
}