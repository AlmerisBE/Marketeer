using Marketeer.UI.Localization.Providers;

namespace Marketeer.Core.MarketListings.Providers;

public class MarketListingsLocalizationProvider : JsonLocalizationProvider {
    protected override string ResourceBasePath => "Marketeer.UI.MarketListings.Resources";
}