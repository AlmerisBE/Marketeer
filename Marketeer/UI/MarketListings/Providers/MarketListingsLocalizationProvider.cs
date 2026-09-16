using Marketeer.UI.Localization.Providers;

namespace Marketeer.UI.MarketListings.Providers;

public class MarketListingsLocalizationProvider : JsonLocalizationProvider {
    protected override string ResourceBasePath => "Marketeer.Core.MarketListings.Resources";
}