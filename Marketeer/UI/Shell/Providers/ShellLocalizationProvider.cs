using Marketeer.UI.Localization.Providers;

namespace Marketeer.UI.Shell.Providers;

public class ShellLocalizationProvider : JsonLocalizationProvider {
    protected override string ResourceBasePath => "Marketeer.UI.Shell.Resources";
}