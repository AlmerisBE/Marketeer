using Marketeer.UI.Localization.Providers;

namespace Marketeer.Core.Configuration.Providers;

public class ConfigurationLocalizationProvider : JsonLocalizationProvider {
    protected override string ResourceBasePath => "Marketeer.Core.Configuration.Resources";
}