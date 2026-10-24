using Dalamud.Bindings.ImGui;
using Marketeer.Core.Configuration.Models;
using Marketeer.UI.Localization.Contracts;
using System;

namespace Marketeer.UI.MarketStrategy.Components;

public class GlobalPricingSettingsComponent {
    private ILocalizationService localizationService;

    public GlobalPricingSettingsComponent(ILocalizationService localizationService) {
        this.localizationService = localizationService;
    }

    public bool Draw(PluginConfiguration config) {
        bool changed = false;

        ImGui.TextUnformatted(this.localizationService.Translate("Label_GlobalHqSettings") ?? "Global HQ Settings");

        bool ignoreHq = config.GlobalIgnoreHq;
        if (ImGui.Checkbox(this.localizationService.Translate("Config_GlobalIgnoreHq") ?? "Ignore HQ Status Globally", ref ignoreHq)) {
            config.GlobalIgnoreHq = ignoreHq;
            changed = true;
        }
        if (ImGui.IsItemHovered()) ImGui.SetTooltip(this.localizationService.Translate("Config_GlobalIgnoreHq_Tooltip"));

        if (!ignoreHq) {
            float premium = (float)config.GlobalHqPremiumPercentage;
            ImGui.SetNextItemWidth(200f);
            if (ImGui.SliderFloat(this.localizationService.Translate("Config_GlobalHqPremium") ?? "HQ Premium (%)", ref premium, 0f, 100f, "%.1f%%")) {
                config.GlobalHqPremiumPercentage = Math.Round(premium, 1);
                changed = true;
            }
            if (ImGui.IsItemHovered()) ImGui.SetTooltip(this.localizationService.Translate("Config_GlobalHqPremium_Tooltip"));
        }

        return changed;
    }
}