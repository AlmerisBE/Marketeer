using Marketeer.Core.Localization.Providers;

namespace Marketeer.UI.CompetitionTracking.Providers;

public class CompetitionLocalizationProvider : JsonLocalizationProvider {
    protected override string ResourceBasePath => "Marketeer.UI.CompetitionTracking.Resources";
}