using Dalamud.Bindings.ImGui;
using Dalamud.Plugin.Services;
using Marketeer.Core.Configuration.Contracts;
using Marketeer.UI.Localization.Contracts;
using Marketeer.UI.Shell.Components;
using Marketeer.UI.Shell.Contracts;
using Marketeer.UI.Themes.Components;
using Marketeer.UI.Themes.Contracts;
using System.Collections.Generic;

namespace Marketeer.UI.Shell.UI;

public class InterfaceConfigMenu : INavigationNode {
    private IConfigurationService configService;
    private ILocalizationService localizationService;

    private ThemeSettingsComponent themeComponent;
    private HotkeySettingsComponent hotkeyComponent;
    private ModifierKeySettingsComponent modifierKeyComponent;

    public string GroupName => this.localizationService.Translate("Group_Configuration") ?? "Configuration";
    public string Name => this.localizationService.Translate("Menu_InterfaceConfig") ?? "Interface & Shortcuts";
    public int Priority => 104;
    public bool HasContent => true;
    public bool DefaultExpanded => false;

    public IEnumerable<INavigationNode> GetChildren() => [];

    public InterfaceConfigMenu(
        IConfigurationService configService,
        ILocalizationService localizationService,
        IKeyState keyState,
        IThemeService themeService) {

        this.configService = configService;
        this.localizationService = localizationService;

        this.themeComponent = new ThemeSettingsComponent(localizationService, themeService);
        this.hotkeyComponent = new HotkeySettingsComponent(localizationService, keyState);
        this.modifierKeyComponent = new ModifierKeySettingsComponent(localizationService);
    }

    public void DrawContent() {
        var config = this.configService.GetConfig();
        bool changed = false;

        if (ImGui.CollapsingHeader(this.localizationService.Translate("Label_ThemeSettings") ?? "Interface Theme", ImGuiTreeNodeFlags.DefaultOpen)) {
            if (this.themeComponent.Draw()) changed = true;
            ImGui.Spacing();
        }

        if (ImGui.CollapsingHeader(this.localizationService.Translate("Label_HotkeySettings") ?? "Dashboard Hotkey", ImGuiTreeNodeFlags.DefaultOpen)) {
            if (this.hotkeyComponent.Draw(config)) changed = true;
            ImGui.Spacing();
        }

        if (ImGui.CollapsingHeader(this.localizationService.Translate("Label_ModifierKeys") ?? "Automation Modifiers", ImGuiTreeNodeFlags.DefaultOpen)) {
            if (this.modifierKeyComponent.Draw(config)) changed = true;
            ImGui.Spacing();
        }

        if (changed) this.configService.Save();
    }
}