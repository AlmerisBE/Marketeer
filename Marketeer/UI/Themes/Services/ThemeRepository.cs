using Dalamud.Plugin;
using Marketeer.Core.Logging.Contracts;
using Marketeer.UI.Themes.Contracts;
using Marketeer.UI.Themes.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Marketeer.UI.Themes.Services;

public class ThemeRepository : IThemeRepository {
    private IDalamudPluginInterface pluginInterface;
    private ILoggerService logger;
    private string themeDirectory;

    public ThemeRepository(IDalamudPluginInterface pluginInterface, ILoggerService logger) {
        this.pluginInterface = pluginInterface;
        this.logger = logger;
        this.themeDirectory = Path.Combine(this.pluginInterface.ConfigDirectory.FullName, "Themes");

        this.EnsureDefaultThemesExist();
    }

    private void EnsureDefaultThemesExist() {
        if (!Directory.Exists(this.themeDirectory)) {
            Directory.CreateDirectory(this.themeDirectory);
        }

        var darkPath = Path.Combine(this.themeDirectory, "Dark.json");
        if (!File.Exists(darkPath)) {
            var darkTheme = new ThemeDefinition {
                Name = "Dark",
                Author = "Almeris",
                Palette = new Dictionary<string, string> {
                    { "TextOnline", "#FFFFFFFF" }, { "TextOffline", "#7F7F7FFF" }, { "TextBusy", "#BFBFBFFF" },
                    { "TextArchived", "#727299FF" }, { "TextDeleted", "#CC6666FF" }, { "TextMarkedForRemoval", "#E54C4CFF" },
                    { "IconDeletedTint", "#FF3333FF" }, { "IconDefaultTint", "#FFFFFFFF" }, { "IconDimmedTint", "#7F7F7FFF" },
                    { "StatusFallbackOnline", "#6DD86DFF" }, { "StatusFallbackOffline", "#7F7F7FFF" }, { "StatusFallbackDeleted", "#CC3333FF" },
                    { "WindowBg", "#262323F2" }, { "Text", "#E5E5E5FF" }, { "ChildBg", "#1E1C1C7F" }, { "PopupBg", "#262323F2" },
                    { "FrameBg", "#333333FF" }, { "FrameBgHovered", "#3F3F3FFF" }, { "FrameBgActive", "#4C4C4CFF" },
                    { "TitleBg", "#1E1C1CFF" }, { "TitleBgActive", "#332626FF" }, { "TitleBgCollapsed", "#191919FF" },
                    { "TableHeaderBg", "#2D2B2BFF" }, { "TableRowBg", "#262323FF" }, { "TableRowBgAlt", "#2D2B2BFF" },
                    { "Border", "#4C3F3FFF" }, { "Tab", "#262323FF" }, { "TabHovered", "#3F3333FF" },
                    { "TabActive", "#4C3F3FFF" }, { "TabUnfocused", "#1E1C1CFF" }, { "TabUnfocusedActive", "#2D2B2BFF" },
                    { "Button", "#3F3333FF" }, { "ButtonHovered", "#593F3FFF" }, { "ButtonActive", "#664C4CFF" }
                }
            };
            File.WriteAllText(darkPath, JsonSerializer.Serialize(darkTheme, new JsonSerializerOptions { WriteIndented = true }));
        }

        var lightPath = Path.Combine(this.themeDirectory, "Light.json");
        if (!File.Exists(lightPath)) {
            var lightTheme = new ThemeDefinition {
                Name = "Light",
                Author = "Almeris",
                Palette = new Dictionary<string, string> {
                    { "TextOnline", "#191919FF" }, { "TextOffline", "#7F7F7FFF" }, { "TextBusy", "#666666FF" },
                    { "TextArchived", "#7F6699FF" }, { "TextDeleted", "#CC1919FF" }, { "TextMarkedForRemoval", "#CC1919FF" },
                    { "IconDeletedTint", "#FF3333FF" }, { "IconDefaultTint", "#FFFFFFFF" }, { "IconDimmedTint", "#7F7F7FFF" },
                    { "StatusFallbackOnline", "#33B233FF" }, { "StatusFallbackOffline", "#999999FF" }, { "StatusFallbackDeleted", "#CC1919FF" },
                    { "WindowBg", "#E8DBC4F9" }, { "Text", "#261C11FF" }, { "ChildBg", "#E0D1BA7F" }, { "PopupBg", "#E8DBC4F9" },
                    { "FrameBg", "#D8C6A5FF" }, { "FrameBgHovered", "#E5D1B2FF" }, { "FrameBgActive", "#CCB799FF" },
                    { "TitleBg", "#D8C6A5FF" }, { "TitleBgActive", "#E5D1B2FF" }, { "TitleBgCollapsed", "#CCB299FF" },
                    { "TableHeaderBg", "#D1BA99FF" }, { "TableRowBg", "#E8DBC4FF" }, { "TableRowBgAlt", "#D8C9AFFF" },
                    { "Border", "#AA8966FF" }, { "Tab", "#D8C6A5FF" }, { "TabHovered", "#E5D1B2FF" },
                    { "TabActive", "#F2E5CCFF" }, { "TabUnfocused", "#CCB799FF" }, { "TabUnfocusedActive", "#D8C6A5FF" },
                    { "Button", "#D8C6A5FF" }, { "ButtonHovered", "#E5D1B2FF" }, { "ButtonActive", "#CCB799FF" }
                }
            };
            File.WriteAllText(lightPath, JsonSerializer.Serialize(lightTheme, new JsonSerializerOptions { WriteIndented = true }));
        }
    }

    public IEnumerable<ThemeDefinition> GetAvailableThemes() {
        var themes = new List<ThemeDefinition>();
        if (!Directory.Exists(this.themeDirectory)) return themes;

        foreach (var file in Directory.GetFiles(this.themeDirectory, "*.json")) {
            try {
                var content = File.ReadAllText(file);
                var theme = JsonSerializer.Deserialize<ThemeDefinition>(content);
                if (theme != null && !string.IsNullOrEmpty(theme.Name)) themes.Add(theme);
            }
            catch (Exception ex) {
                this.logger.Error(ex, $"Failed to load theme file: {file}");
            }
        }
        return themes;
    }

    public ThemeDefinition? GetTheme(string name) {
        foreach (var theme in this.GetAvailableThemes()) {
            if (theme.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase)) return theme;
        }
        return null;
    }
}