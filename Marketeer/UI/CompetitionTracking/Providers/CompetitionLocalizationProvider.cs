using Marketeer.UI.Localization.Providers;

namespace Marketeer.UI.CompetitionTracking.Providers;

public class CompetitionLocalizationProvider : JsonLocalizationProvider {
    protected override string ResourceBasePath => "Marketeer.Core.CompetitionTracking.Resources";
}