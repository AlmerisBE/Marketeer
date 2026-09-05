using Marketeer.UI.Localization.Providers;

namespace Marketeer.Core.CompetitionTracking.Providers;

public class CompetitionLocalizationProvider : JsonLocalizationProvider {
    protected override string ResourceBasePath => "Marketeer.Core.CompetitionTracking.Resources";
}