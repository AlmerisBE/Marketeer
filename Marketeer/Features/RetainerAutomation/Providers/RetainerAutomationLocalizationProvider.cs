using Marketeer.Features.Localization.Providers;

namespace Marketeer.Features.RetainerAutomation.Providers;

public class RetainerAutomationLocalizationProvider : JsonLocalizationProvider {
    protected override string ResourceBasePath => "Marketeer.Features.RetainerAutomation.Resources";
}