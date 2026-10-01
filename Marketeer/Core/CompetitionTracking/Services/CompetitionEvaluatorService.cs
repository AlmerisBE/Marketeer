using Marketeer.Core.CompetitionTracking.Contracts;
using Marketeer.Core.CompetitionTracking.Models;
using Marketeer.Core.Configuration.Contracts;
using Marketeer.Core.Configuration.Models;
using Marketeer.Core.MarketPricing.Models;
using Marketeer.Core.MarketStrategy.Contracts;
using Marketeer.UI.CompetitionTracking.Models;
using System.Linq;

namespace Marketeer.Core.CompetitionTracking.Services;

public class CompetitionEvaluatorService : ICompetitionEvaluatorService {
    private IConfigurationService configService;
    private IWhitelistManagerService whitelistManager;
    private IMarketAnomalyDetector anomalyDetector;

    public CompetitionEvaluatorService(
        IConfigurationService configService,
        IWhitelistManagerService whitelistManager,
        IMarketAnomalyDetector anomalyDetector) {

        this.configService = configService;
        this.whitelistManager = whitelistManager;
        this.anomalyDetector = anomalyDetector;
    }

    public bool TryEvaluateListing(RetainerListing listing, MarketItemPricing pricing, string characterName, string resolvedItemName, out UndercutItem? undercutResult) {
        undercutResult = null;

        if (pricing.Listings == null || !pricing.Listings.Any()) return false;

        var config = this.configService.GetConfig();

        var anomalyReport = this.anomalyDetector.EvaluateMarket(pricing, listing.IsHq);

        var validCompetitors = pricing.Listings
            .Where(l => l.IsHq == listing.IsHq)
            .Where(l => !(this.whitelistManager.IsWhitelisted(l.RetainerName) && config.CompetitorWhitelistBehavior == WhitelistBehavior.Ignore))
            .OrderBy(l => l.Price)
            .ToList();

        if (validCompetitors.Count == 0) return false;

        var lowestCompetitor = validCompetitors.First();

        if (listing.CurrentPrice < lowestCompetitor.Price) return false;
        if (listing.CurrentPrice == lowestCompetitor.Price) return false;

        uint targetPrice = lowestCompetitor.Price - 1;
        if (this.whitelistManager.IsWhitelisted(lowestCompetitor.RetainerName) && config.CompetitorWhitelistBehavior == WhitelistBehavior.MatchPrice) {
            targetPrice = lowestCompetitor.Price;
        }

        var suggestedAction = PricingAction.UpdatePrice;

        // Defensive null check added here
        if (anomalyReport != null && anomalyReport.IsAnomalyDetected) {
            if (config.AnomalyStrategy == AnomalyDefenseStrategy.HoldPrice || config.AnomalyStrategy == AnomalyDefenseStrategy.AlertAndPause) {
                targetPrice = listing.CurrentPrice;
                suggestedAction = PricingAction.KeepPrice;
            }
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
            SuggestedAction = suggestedAction,
            AnomalyData = anomalyReport
        };

        return true;
    }
}