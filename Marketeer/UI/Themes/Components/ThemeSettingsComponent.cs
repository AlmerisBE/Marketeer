using Dalamud.Bindings.ImGui;
using Marketeer.UI.Localization.Contracts;
using Marketeer.UI.Themes.Contracts;
using System.Linq;

namespace Marketeer.UI.Themes.Components;

public class ThemeSettingsComponent {
    private ILocalizationService localizationService;
    private IThemeService themeService;

    public ThemeSettingsComponent(ILocalizationService localizationService, IThemeService themeService) {
        this.localizationService = localizationService;
        this.themeService = themeService;
    }

    public bool Draw() {
        bool isChanged = false;

        ImGui.TextUnformatted(this.localizationService.Translate("Config_ThemeLabel") ?? "Interface Theme:");
        ImGui.SetNextItemWidth(250f);

        var themes = this.themeService.GetAvailableThemes().ToList();
        var currentTheme = this.themeService.CurrentTheme;

        string preview = string.IsNullOrEmpty(currentTheme.Name) ? "Default" : currentTheme.Name;

        if (ImGui.BeginCombo("##ThemeSelect", preview)) {
            foreach (var theme in themes) {
                bool isSelected = theme.Name == currentTheme.Name;
                if (ImGui.Selectable(theme.Name, isSelected)) {
                    this.themeService.SetTheme(theme.Name);
                    isChanged = true;
                }
                if (isSelected) ImGui.SetItemDefaultFocus();
            }
            ImGui.EndCombo();
        }
        if (ImGui.IsItemHovered()) ImGui.SetTooltip(this.localizationService.Translate("Config_Theme_Tooltip"));

        return isChanged;
    }
}