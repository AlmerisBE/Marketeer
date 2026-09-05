using Marketeer.UI.Localization.Providers;

namespace Marketeer.UI.Guidance.Providers;

public class GuidanceLocalizationProvider : JsonLocalizationProvider {
    protected override string ResourceBasePath => "Marketeer.UI.Guidance.Resources";
}