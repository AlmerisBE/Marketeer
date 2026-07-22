using Marketeer.Core.Localization.Providers;

namespace Marketeer.UI.MarketListings.Providers;

public class MarketListingsLocalizationProvider : JsonLocalizationProvider {
    protected override string ResourceBasePath => "Marketeer.UI.MarketListings.Resources";
}