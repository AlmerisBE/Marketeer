using Marketeer.Features.Localization.Providers;

namespace Marketeer.Features.UndercutTracking.Providers;

public class UndercutLocalizationProvider : JsonLocalizationProvider {
    protected override string ResourceBasePath => "Marketeer.Features.UndercutTracking.Resources";
}