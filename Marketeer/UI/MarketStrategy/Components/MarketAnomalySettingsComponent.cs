using Dalamud.Bindings.ImGui;
using Marketeer.Core.Configuration.Models;
using Marketeer.Core.MarketStrategy.Models;
using Marketeer.UI.Localization.Contracts;
using System;

namespace Marketeer.UI.MarketStrategy.Components;

public class MarketAnomalySettingsComponent {
    private ILocalizationService localizationService;

    public MarketAnomalySettingsComponent(ILocalizationService localizationService) {
        this.localizationService = localizationService;
    }

    public bool Draw(PluginConfiguration config) {
        bool changed = false;

        bool isEnabled = config.EnableAnomalyProtection;
        if (ImGui.Checkbox(this.localizationService.Translate("Config_EnableAnomalyProtection") ?? "Enable Market Crash Protection", ref isEnabled)) {
            config.EnableAnomalyProtection = isEnabled;
            changed = true;
        }
        if (ImGui.IsItemHovered()) ImGui.SetTooltip(this.localizationService.Translate("Config_EnableAnomalyProtection_Tooltip"));

        if (isEnabled) {
            ImGui.Spacing();
            ImGui.Indent();

            ImGui.TextUnformatted(this.localizationService.Translate("Config_AnomalyCrashThreshold") ?? "Crash Threshold (%)");
            ImGui.SetNextItemWidth(250f);

            int thresholdPct = (int)(config.AnomalyCrashThreshold * 100);
            if (ImGui.SliderInt("##CrashThreshold", ref thresholdPct, 1, 99)) {
                config.AnomalyCrashThreshold = thresholdPct / 100.0;
                changed = true;
            }
            if (ImGui.IsItemHovered()) ImGui.SetTooltip(this.localizationService.Translate("Config_AnomalyCrashThreshold_Tooltip"));

            ImGui.Spacing();

            ImGui.TextUnformatted(this.localizationService.Translate("Config_AnomalyStrategy") ?? "Defense Strategy");
            ImGui.SetNextItemWidth(250f);

            if (ImGui.BeginCombo("##AnomalyStrategy", this.localizationService.Translate($"Config_AnomalyStrategy_{config.AnomalyStrategy}"))) {
                foreach (AnomalyDefenseStrategy strategy in Enum.GetValues(typeof(AnomalyDefenseStrategy))) {
                    if (ImGui.Selectable(this.localizationService.Translate($"Config_AnomalyStrategy_{strategy}"), config.AnomalyStrategy == strategy)) {
                        config.AnomalyStrategy = strategy;
                        changed = true;
                    }
                }
                ImGui.EndCombo();
            }
            if (ImGui.IsItemHovered()) ImGui.SetTooltip(this.localizationService.Translate("Config_AnomalyStrategy_Tooltip"));

            ImGui.Unindent();
        }

        return changed;
    }
}