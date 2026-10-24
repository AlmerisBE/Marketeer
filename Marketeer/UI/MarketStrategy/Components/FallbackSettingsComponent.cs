using Dalamud.Bindings.ImGui;
using Marketeer.Core.Configuration.Models;
using Marketeer.Core.MarketStrategy.Models;
using Marketeer.UI.Localization.Contracts;
using System;

namespace Marketeer.UI.MarketStrategy.Components;

public class FallbackSettingsComponent {
    private ILocalizationService localizationService;

    public FallbackSettingsComponent(ILocalizationService localizationService) {
        this.localizationService = localizationService;
    }

    public bool Draw(PluginConfiguration config) {
        bool changed = false;

        ImGui.TextUnformatted(this.localizationService.Translate("Config_FallbackMode") ?? "Fallback Strategy");
        ImGui.SetNextItemWidth(250f);
        if (ImGui.BeginCombo("##FallbackMode", this.localizationService.Translate($"Config_FallbackMode_{config.EmptyMarketFallbackMode}"))) {
            foreach (FallbackPricingMode mode in Enum.GetValues(typeof(FallbackPricingMode))) {
                if (ImGui.Selectable(this.localizationService.Translate($"Config_FallbackMode_{mode}"), config.EmptyMarketFallbackMode == mode)) {
                    config.EmptyMarketFallbackMode = mode;
                    changed = true;
                }
            }
            ImGui.EndCombo();
        }
        if (ImGui.IsItemHovered()) ImGui.SetTooltip(this.localizationService.Translate("Config_FallbackMode_Tooltip"));

        ImGui.Spacing();

        ImGui.TextUnformatted(this.localizationService.Translate("Config_FallbackMultiplier") ?? "Fallback Multiplier");
        ImGui.SetNextItemWidth(150f);
        float multiplier = (float)config.EmptyMarketFallbackMultiplier;
        if (ImGui.InputFloat("##FallbackMultiplier", ref multiplier, 0.1f, 1.0f, "%.1f")) {
            if (multiplier < 1.0f) multiplier = 1.0f;
            config.EmptyMarketFallbackMultiplier = Math.Round(multiplier, 1);
            changed = true;
        }
        if (ImGui.IsItemHovered()) ImGui.SetTooltip(this.localizationService.Translate("Config_FallbackMultiplier_Tooltip"));

        return changed;
    }
}