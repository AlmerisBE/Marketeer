using Marketeer.UI.Localization.Providers;

namespace Marketeer.UI.Themes.Providers;

public class ThemesLocalizationProvider : JsonLocalizationProvider {
    protected override string ResourceBasePath => "Marketeer.UI.Themes.Resources";
}