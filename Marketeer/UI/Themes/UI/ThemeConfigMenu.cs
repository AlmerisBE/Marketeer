using Dalamud.Bindings.ImGui;
using Marketeer.Core.Configuration.Contracts;
using Marketeer.UI.Localization.Contracts;
using Marketeer.UI.Shell.Contracts;
using Marketeer.UI.Themes.Contracts;
using System.Collections.Generic;

namespace Marketeer.UI.Themes.UI;

public class ThemeConfigMenu : INavigationNode {
    private IConfigurationService configurationService;
    private ILocalizationService localizationService;
    private IThemeService themeService;

    public string GroupName => this.localizationService.Translate("Group_Configuration");
    public string Name => this.localizationService.Translate("Config_Tab_Theme");
    public int Priority => 103;
    public bool HasContent => true;
    public bool DefaultExpanded => false;

    public IEnumerable<INavigationNode> GetChildren() => [];

    public ThemeConfigMenu(IConfigurationService configurationService, ILocalizationService localizationService, IThemeService themeService) {
        this.configurationService = configurationService;
        this.localizationService = localizationService;
        this.themeService = themeService;
    }

    public void DrawContent() {
        var config = this.configurationService.GetConfig();
        bool isChanged = false;

        ImGui.TextUnformatted(this.Name);
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.TextUnformatted(this.localizationService.Translate("Config_ThemeLabel"));

        string currentDisplayName = config.SelectedTheme == "Default" ? this.localizationService.Translate("Theme_Default") : config.SelectedTheme;

        if (ImGui.BeginCombo("##themeCombo", currentDisplayName)) {
            foreach (var theme in this.themeService.GetAvailableThemes()) {
                string displayName = theme.Name == "Default" ? this.localizationService.Translate("Theme_Default") : theme.Name;

                if (ImGui.Selectable(displayName, config.SelectedTheme == theme.Name)) {
                    config.SelectedTheme = theme.Name;
                    this.themeService.SetTheme(theme.Name);
                    isChanged = true;
                }
            }
            ImGui.EndCombo();
        }

        if (isChanged) this.configurationService.Save();
    }
}