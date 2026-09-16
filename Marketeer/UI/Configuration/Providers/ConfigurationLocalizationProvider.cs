using Marketeer.UI.Localization.Providers;

namespace Marketeer.UI.Configuration.Providers;

public class ConfigurationLocalizationProvider : JsonLocalizationProvider {
    protected override string ResourceBasePath => "Marketeer.Core.Configuration.Resources";
}