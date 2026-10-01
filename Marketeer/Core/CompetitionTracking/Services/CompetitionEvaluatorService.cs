using Marketeer.Core.CompetitionTracking.Contracts;
using Marketeer.Core.CompetitionTracking.Models;
using Marketeer.Core.Configuration.Contracts;
using Marketeer.Core.Configuration.Models;
using Marketeer.Core.MarketPricing.Models;
using Marketeer.UI.CompetitionTracking.Models;
using System.Linq;

namespace Marketeer.Core.CompetitionTracking.Services;

public class CompetitionEvaluatorService : ICompetitionEvaluatorService {
    private IConfigurationService configService;
    private IWhitelistManagerService whitelistManager;

    public CompetitionEvaluatorService(IConfigurationService configService, IWhitelistManagerService whitelistManager) {
        this.configService = configService;
        this.whitelistManager = whitelistManager;
    }

    public bool TryEvaluateListing(RetainerListing listing, MarketItemPricing pricing, string characterName, string resolvedItemName, out UndercutItem? undercutResult) {
        undercutResult = null;

        if (pricing.Listings == null || !pricing.Listings.Any()) return false;

        var config = this.configService.GetConfig();

        // 1. Isolate qualities and completely filter out 'Ignored' whitelisted retainers
        var validCompetitors = pricing.Listings
            .Where(l => l.IsHq == listing.IsHq)
            .Where(l => !(this.whitelistManager.IsWhitelisted(l.RetainerName) && config.CompetitorWhitelistBehavior == WhitelistBehavior.Ignore))
            .OrderBy(l => l.Price)
            .ToList();

        if (validCompetitors.Count == 0) return false;

        var lowestCompetitor = validCompetitors.First();

        // Cache Lag Prevention
        if (listing.CurrentPrice < lowestCompetitor.Price) return false;

        // Equality Exemption
        if (listing.CurrentPrice == lowestCompetitor.Price) return false;

        // Determine target price based on MatchPrice behavior
        uint targetPrice = lowestCompetitor.Price - 1;
        if (this.whitelistManager.IsWhitelisted(lowestCompetitor.RetainerName) && config.CompetitorWhitelistBehavior == WhitelistBehavior.MatchPrice) {
            targetPrice = lowestCompetitor.Price;
        }

        undercutResult = new UndercutItem {
            SlotIndex = listing.SlotIndex,
            ItemId = listing.ItemId,
            ItemName = resolvedItemName,
            Quantity = listing.Quantity,
            RetainerName = listing.RetainerName,
            Price = listing.CurrentPrice,
            OurPrice = listing.CurrentPrice,
            ServerCheapestPrice = lowestCompetitor.Price,
            TargetPrice = targetPrice,
            CompetitorName = lowestCompetitor.RetainerName ?? "Unknown",
            CharacterName = characterName,
            SuggestedAction = PricingAction.UpdatePrice
        };

        return true;
    }
}