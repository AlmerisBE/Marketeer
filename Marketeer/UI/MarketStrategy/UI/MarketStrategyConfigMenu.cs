using Dalamud.Bindings.ImGui;
using Marketeer.Core.Configuration.Contracts;
using Marketeer.Core.Configuration.Models;
using Marketeer.UI.Localization.Contracts;
using Marketeer.UI.Shell.Contracts;
using System;
using System.Collections.Generic;

namespace Marketeer.UI.MarketStrategy.UI;

public class MarketStrategyConfigMenu : INavigationNode {
    private IConfigurationService configService;
    private ILocalizationService localization;

    public string GroupName => this.localization.Translate("Group_Configuration") ?? "Configuration";
    public string Name => this.localization.Translate("Menu_MarketStrategy") ?? "Market Strategy";
    public int Priority => 110;
    public bool HasContent => true;
    public bool DefaultExpanded => false;
    public IEnumerable<INavigationNode> GetChildren() => [];

    public MarketStrategyConfigMenu(IConfigurationService configService, ILocalizationService localization) {
        this.configService = configService;
        this.localization = localization;
    }

    public void DrawContent() {
        var config = this.configService.GetConfig();
        bool changed = false;

        bool isEnabled = config.EnableAnomalyProtection;
        if (ImGui.Checkbox(this.localization.Translate("Config_EnableAnomalyProtection") ?? "Enable Market Crash Protection", ref isEnabled)) {
            config.EnableAnomalyProtection = isEnabled;
            changed = true;
        }

        if (isEnabled) {
            ImGui.Spacing();
            ImGui.Indent();

            int thresholdPct = (int)(config.AnomalyCrashThreshold * 100);
            if (ImGui.SliderInt(this.localization.Translate("Config_AnomalyCrashThreshold") ?? "Crash Threshold (%)", ref thresholdPct, 1, 99)) {
                config.AnomalyCrashThreshold = thresholdPct / 100.0;
                changed = true;
            }

            if (ImGui.BeginCombo(this.localization.Translate("Config_AnomalyStrategy") ?? "Defense Strategy", this.localization.Translate($"Config_AnomalyStrategy_{config.AnomalyStrategy}"))) {
                foreach (AnomalyDefenseStrategy strategy in Enum.GetValues(typeof(AnomalyDefenseStrategy))) {
                    if (ImGui.Selectable(this.localization.Translate($"Config_AnomalyStrategy_{strategy}"), config.AnomalyStrategy == strategy)) {
                        config.AnomalyStrategy = strategy;
                        changed = true;
                    }
                }
                ImGui.EndCombo();
            }

            ImGui.Unindent();
        }

        if (changed) this.configService.Save();
    }
}