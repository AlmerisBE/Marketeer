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
    public int Priority => 105;
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

        if (enableDelay) {
            ImGui.Indent();
            int min = config.AutomationDelayMin;
            int max = config.AutomationDelayMax;

            ImGui.SetNextItemWidth(100f);
            if (ImGui.InputInt(this.localizationService.Translate("Config_AutomationDelayMin"), ref min)) {
                if (min < 0) min = 0;
                config.AutomationDelayMin = min;
                if (min > config.AutomationDelayMax) config.AutomationDelayMax = min;
                isChanged = true;
            }

            ImGui.SetNextItemWidth(100f);
            if (ImGui.InputInt(this.localizationService.Translate("Config_AutomationDelayMax"), ref max)) {
                if (max < config.AutomationDelayMin) max = config.AutomationDelayMin;
                config.AutomationDelayMax = max;
                isChanged = true;
            }
            ImGui.Unindent();
        }

        ImGui.Spacing();

        bool enableChatNotifications = config.EnableChatNotifications;
        if (ImGui.Checkbox(this.localizationService.Translate("Config_EnableChatNotifications"), ref enableChatNotifications)) {
            config.EnableChatNotifications = enableChatNotifications;
            isChanged = true;
        }

        ImGui.Spacing();

        bool enableDebugMode = config.EnableDebugMode;
        if (ImGui.Checkbox(this.localizationService.Translate("Config_EnableDebugMode"), ref enableDebugMode)) {
            config.EnableDebugMode = enableDebugMode;
            isChanged = true;
        }

        if (isChanged) this.configurationService.Save();
    }
}