using Marketeer.Core.CompetitionTracking.Contracts;
using Marketeer.Core.CompetitionTracking.Models;
using Marketeer.Core.MarketPricing.Models;
using Marketeer.UI.CompetitionTracking.Models;
using System.Linq;

namespace Marketeer.Core.CompetitionTracking.Services;

public class CompetitionEvaluatorService : ICompetitionEvaluatorService {
    public bool TryEvaluateListing(RetainerListing listing, MarketItemPricing pricing, string characterName, string resolvedItemName, out UndercutItem? undercutResult) {
        undercutResult = null;

        if (pricing.Listings == null || !pricing.Listings.Any()) return false;

        // Isolate qualities to prevent false comparisons (e.g., comparing NQ with HQ)
        var validCompetitors = pricing.Listings
            .Where(l => l.IsHq == listing.IsHq)
            .OrderBy(l => l.Price)
            .ToList();

        if (validCompetitors.Count == 0) return false;

        var lowestCompetitor = validCompetitors.First();

        // Cache Lag Prevention: If our ingame price is lower than the API's lowest, we lead the market
        if (listing.CurrentPrice < lowestCompetitor.Price) return false;

        // Equality Exemption: If prices match perfectly, we are aligned
        if (listing.CurrentPrice == lowestCompetitor.Price) return false;

        // Strict undercut detected
        undercutResult = new UndercutItem {
            SlotIndex = listing.SlotIndex,
            ItemId = listing.ItemId,
            ItemName = resolvedItemName,
            Quantity = listing.Quantity,
            RetainerName = listing.RetainerName,
            Price = listing.CurrentPrice,
            OurPrice = listing.CurrentPrice,
            ServerCheapestPrice = lowestCompetitor.Price,
            TargetPrice = lowestCompetitor.Price - 1,
            CompetitorName = lowestCompetitor.RetainerName ?? "Unknown",
            CharacterName = characterName,
            SuggestedAction = PricingAction.UpdatePrice
        };

        return true;
    }
}