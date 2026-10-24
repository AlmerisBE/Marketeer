using Dalamud.Bindings.ImGui;
using Marketeer.Core.Configuration.Contracts;
using Marketeer.UI.Localization.Contracts;
using Marketeer.UI.Shell.Components;
using Marketeer.UI.Shell.Contracts;
using Marketeer.UI.Universalis.Components;
using System.Collections.Generic;

namespace Marketeer.UI.Shell.UI;

public class SystemConfigMenu : INavigationNode {
    private IConfigurationService configService;
    private ILocalizationService localizationService;

    private AutomationSettingsComponent automationComponent;
    private NotificationSettingsComponent notificationComponent;
    private UniversalisSettingsComponent universalisComponent;

    public string GroupName => this.localizationService.Translate("Group_Configuration") ?? "Configuration";
    public string Name => this.localizationService.Translate("Menu_SystemConfig") ?? "Automation & System";
    public int Priority => 105;
    public bool HasContent => true;
    public bool DefaultExpanded => false;

    public IEnumerable<INavigationNode> GetChildren() => [];

    public SystemConfigMenu(IConfigurationService configService, ILocalizationService localizationService) {
        this.configService = configService;
        this.localizationService = localizationService;

        this.automationComponent = new AutomationSettingsComponent(localizationService);
        this.notificationComponent = new NotificationSettingsComponent(localizationService);
        this.universalisComponent = new UniversalisSettingsComponent(localizationService);
    }

    public void DrawContent() {
        var config = this.configService.GetConfig();
        bool changed = false;

        if (ImGui.CollapsingHeader(this.localizationService.Translate("Label_AutomationSettings") ?? "Automation Delays", ImGuiTreeNodeFlags.DefaultOpen)) {
            if (this.automationComponent.Draw(config)) changed = true;
            ImGui.Spacing();
        }

        if (ImGui.CollapsingHeader(this.localizationService.Translate("Label_UniversalisSettings") ?? "Universalis API", ImGuiTreeNodeFlags.DefaultOpen)) {
            if (this.universalisComponent.Draw(config)) changed = true;
            ImGui.Spacing();
        }

        if (ImGui.CollapsingHeader(this.localizationService.Translate("Label_NotificationSettings") ?? "Notifications & Debug", ImGuiTreeNodeFlags.DefaultOpen)) {
            if (this.notificationComponent.Draw(config)) changed = true;
            ImGui.Spacing();
        }

        if (changed) this.configService.Save();
    }
}