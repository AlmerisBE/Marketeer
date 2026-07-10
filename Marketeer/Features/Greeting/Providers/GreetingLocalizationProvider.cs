using Marketeer.Features.Localization.Providers;

namespace Marketeer.Features.Greeting.Providers;

public class GreetingLocalizationProvider : JsonLocalizationProvider {
    // The base logical path. The abstract class will append ".en.json", ".fr.json", etc.
    protected override string ResourceBasePath => "Marketeer.Features.Greeting.Resources";
}