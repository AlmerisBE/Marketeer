using Dalamud.Bindings.ImGui;
using Marketeer.Core.Configuration.Models;
using Marketeer.UI.Localization.Contracts;

namespace Marketeer.UI.Shell.Components;

public class AutomationSettingsComponent {
    private ILocalizationService localizationService;

    public AutomationSettingsComponent(ILocalizationService localizationService) {
        this.localizationService = localizationService;
    }

    public bool Draw(PluginConfiguration config) {
        bool isChanged = false;
        bool enableDelay = config.EnableAutomationDelay;

        if (ImGui.Checkbox(this.localizationService.Translate("Config_AutomationDelayToggle") ?? "Enable randomized delay", ref enableDelay)) {
            config.EnableAutomationDelay = enableDelay;
            isChanged = true;
        }
        if (ImGui.IsItemHovered()) ImGui.SetTooltip(this.localizationService.Translate("Config_AutomationDelay_Tooltip"));

        if (enableDelay) {
            ImGui.Indent();
            ImGui.TextUnformatted(this.localizationService.Translate("Config_AutomationDelayMin") ?? "Min Delay (s)");
            ImGui.SetNextItemWidth(150f);
            int min = config.AutomationDelayMin;

            if (ImGui.InputInt("##AutomationDelayMin", ref min)) {
                if (min < 0) min = 0;
                config.AutomationDelayMin = min;
                if (min > config.AutomationDelayMax) config.AutomationDelayMax = min;
                isChanged = true;
            }

            ImGui.Spacing();
            ImGui.TextUnformatted(this.localizationService.Translate("Config_AutomationDelayMax") ?? "Max Delay (s)");
            ImGui.SetNextItemWidth(150f);
            int max = config.AutomationDelayMax;

            if (ImGui.InputInt("##AutomationDelayMax", ref max)) {
                if (max < config.AutomationDelayMin) max = config.AutomationDelayMin;
                config.AutomationDelayMax = max;
                isChanged = true;
            }
            ImGui.Unindent();
        }

        return isChanged;
    }
}