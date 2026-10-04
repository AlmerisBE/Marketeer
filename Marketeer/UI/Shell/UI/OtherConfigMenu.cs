using Dalamud.Bindings.ImGui;
using Marketeer.Core.Configuration.Contracts;
using Marketeer.UI.Localization.Contracts;
using Marketeer.UI.Shell.Contracts;
using System.Collections.Generic;

namespace Marketeer.UI.Shell.UI;

public class OtherConfigMenu : INavigationNode {
    private IConfigurationService configurationService;
    private ILocalizationService localizationService;

    public string GroupName => this.localizationService.Translate("Group_Configuration");
    public string Name => this.localizationService.Translate("Config_Tab_Other");
    public int Priority => 106;
    public bool HasContent => true;
    public bool DefaultExpanded => false;

    public IEnumerable<INavigationNode> GetChildren() => [];

    public OtherConfigMenu(IConfigurationService configurationService, ILocalizationService localizationService) {
        this.configurationService = configurationService;
        this.localizationService = localizationService;
    }

    public void DrawContent() {
        var config = this.configurationService.GetConfig();
        bool isChanged = false;

        ImGui.TextUnformatted(this.Name);
        ImGui.Separator();
        ImGui.Spacing();

        bool enableDelay = config.EnableAutomationDelay;
        if (ImGui.Checkbox(this.localizationService.Translate("Config_AutomationDelayToggle"), ref enableDelay)) {
            config.EnableAutomationDelay = enableDelay;
            isChanged = true;
        }
        if (ImGui.IsItemHovered()) ImGui.SetTooltip(this.localizationService.Translate("Config_AutomationDelay_Tooltip"));

        if (enableDelay) {
            ImGui.Indent();

            ImGui.TextUnformatted(this.localizationService.Translate("Config_AutomationDelayMin"));
            ImGui.SetNextItemWidth(150f);

            int min = config.AutomationDelayMin;
            int max = config.AutomationDelayMax;

            if (ImGui.InputInt("##AutomationDelayMin", ref min)) {
                if (min < 0) min = 0;
                config.AutomationDelayMin = min;
                if (min > config.AutomationDelayMax) config.AutomationDelayMax = min;
                isChanged = true;
            }

            ImGui.Spacing();

            ImGui.TextUnformatted(this.localizationService.Translate("Config_AutomationDelayMax"));
            ImGui.SetNextItemWidth(150f);
            if (ImGui.InputInt("##AutomationDelayMax", ref max)) {
                if (max < config.AutomationDelayMin) max = config.AutomationDelayMin;
                config.AutomationDelayMax = max;
                isChanged = true;
            }
            ImGui.Unindent();
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        bool enableChatNotifications = config.EnableChatNotifications;
        if (ImGui.Checkbox(this.localizationService.Translate("Config_EnableChatNotifications"), ref enableChatNotifications)) {
            config.EnableChatNotifications = enableChatNotifications;
            isChanged = true;
        }
        if (ImGui.IsItemHovered()) ImGui.SetTooltip(this.localizationService.Translate("Config_EnableChatNotifications_Tooltip"));

        ImGui.Spacing();

        bool enableDebugMode = config.EnableDebugMode;
        if (ImGui.Checkbox(this.localizationService.Translate("Config_EnableDebugMode"), ref enableDebugMode)) {
            config.EnableDebugMode = enableDebugMode;
            isChanged = true;
        }
        if (ImGui.IsItemHovered()) ImGui.SetTooltip(this.localizationService.Translate("Config_EnableDebugMode_Tooltip"));

        if (isChanged) this.configurationService.Save();
    }
}