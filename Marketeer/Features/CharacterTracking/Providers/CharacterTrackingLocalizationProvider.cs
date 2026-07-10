using Marketeer.Features.Localization.Providers;

namespace Marketeer.Features.CharacterTracking.Providers;

public class CharacterTrackingLocalizationProvider : JsonLocalizationProvider {
    protected override string ResourceBasePath => "Marketeer.Features.CharacterTracking.Resources";
}