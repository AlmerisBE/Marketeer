using Marketeer.UI.Localization.Providers;

namespace Marketeer.Core.MarketWatch.Providers;

public class MarketWatchLocalizationProvider : JsonLocalizationProvider {
    protected override string ResourceBasePath => "Marketeer.UI.MarketWatch.Resources";
}