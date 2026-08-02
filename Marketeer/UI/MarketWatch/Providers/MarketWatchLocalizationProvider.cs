using Marketeer.Core.Localization.Providers;

namespace Marketeer.UI.MarketWatch.Providers;

public class MarketWatchLocalizationProvider : JsonLocalizationProvider {
    protected override string ResourceBasePath => "Marketeer.UI.MarketWatch.Resources";
}