using Marketeer.UI.Themes.Models;
using System.Collections.Generic;

namespace Marketeer.UI.Themes.Contracts;

public interface IThemeRepository {
    IEnumerable<ThemeDefinition> GetAvailableThemes();
    ThemeDefinition? GetTheme(string name);
}