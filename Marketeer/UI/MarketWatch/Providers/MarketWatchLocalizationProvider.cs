using Marketeer.UI.Localization.Providers;

namespace Marketeer.UI.MarketWatch.Providers;

public class MarketWatchLocalizationProvider : JsonLocalizationProvider {
    protected override string ResourceBasePath => "Marketeer.Core.MarketWatch.Resources";
}