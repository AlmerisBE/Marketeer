using Dalamud.Bindings.ImGui;
using Marketeer.Core.Configuration.Contracts;
using Marketeer.Core.Logging.Contracts;
using Marketeer.UI.Themes.Contracts;
using Marketeer.UI.Themes.Models;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Marketeer.UI.Themes.Services;

public class ThemeService : IThemeService {
    private IThemeRepository repository;
    private IConfigurationService configService;
    private ILoggerService logger;

    public ThemeDefinition CurrentTheme { get; private set; }

    public ThemeService(IThemeRepository repository, IConfigurationService configService, ILoggerService logger) {
        this.repository = repository;
        this.configService = configService;
        this.logger = logger;

        this.CurrentTheme = new ThemeDefinition { Name = "Default" };

        var selectedTheme = this.configService.GetConfig().SelectedTheme;
        if (string.IsNullOrEmpty(selectedTheme)) selectedTheme = "Default";

        this.SetTheme(selectedTheme);
    }

    public IEnumerable<ThemeDefinition> GetAvailableThemes() => this.repository.GetAvailableThemes();

    public void SetTheme(string themeName) {
        var theme = this.repository.GetTheme(themeName);
        if (theme != null) this.CurrentTheme = theme;
        else {
            this.logger.Warning($"Theme '{themeName}' could not be loaded. Falling back to default.");
            this.CurrentTheme = this.repository.GetTheme("Default") ?? new ThemeDefinition { Name = "Default" };
        }
    }

    public Vector4 GetCustomColor(string key, Vector4 fallback) {
        if (this.CurrentTheme.Palette.TryGetValue(key, out var hexColor)) {
            return this.ParseHex(hexColor);
        }
        return fallback;
    }

    public IDisposable ApplyTheme() {
        int pushCount = 0;
        foreach (var kvp in this.CurrentTheme.Palette) {
            if (Enum.TryParse<ImGuiCol>(kvp.Key, out var imguiCol)) {
                ImGui.PushStyleColor(imguiCol, this.ParseHex(kvp.Value));
                pushCount++;
            }
        }
        return new ThemeScope(pushCount);
    }

    private Vector4 ParseHex(string hex) {
        if (string.IsNullOrEmpty(hex) || !hex.StartsWith("#")) return Vector4.One;

        try {
            float r = Convert.ToInt32(hex.Substring(1, 2), 16) / 255f;
            float g = Convert.ToInt32(hex.Substring(3, 2), 16) / 255f;
            float b = Convert.ToInt32(hex.Substring(5, 2), 16) / 255f;
            float a = hex.Length == 9 ? Convert.ToInt32(hex.Substring(7, 2), 16) / 255f : 1.0f;
            return new Vector4(r, g, b, a);
        }
        catch {
            return Vector4.One;
        }
    }

    private class ThemeScope : IDisposable {
        private int popCount;
        public ThemeScope(int count) => this.popCount = count;
        public void Dispose() {
            if (this.popCount > 0) ImGui.PopStyleColor(this.popCount);
        }
    }
}