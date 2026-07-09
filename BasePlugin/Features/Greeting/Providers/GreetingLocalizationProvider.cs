using BasePlugin.Features.Localization.Providers;

namespace BasePlugin.Features.Greeting.Providers;

public class GreetingLocalizationProvider : JsonLocalizationProvider {
    // The base logical path. The abstract class will append ".en.json", ".fr.json", etc.
    protected override string ResourceBasePath => "BasePlugin.Features.Greeting.Resources";
}