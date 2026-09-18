using System.Collections.Generic;

namespace Marketeer.UI.Themes.Models;

public class ThemeDefinition {
    public string Name { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public Dictionary<string, string> Palette { get; set; } = new();
}