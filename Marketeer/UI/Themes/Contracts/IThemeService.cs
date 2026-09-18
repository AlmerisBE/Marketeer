using Marketeer.UI.Themes.Models;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Marketeer.UI.Themes.Contracts;

public interface IThemeService {
    ThemeDefinition CurrentTheme { get; }
    IEnumerable<ThemeDefinition> GetAvailableThemes();
    void SetTheme(string themeName);
    Vector4 GetCustomColor(string key, Vector4 fallback);
    IDisposable ApplyTheme();
}