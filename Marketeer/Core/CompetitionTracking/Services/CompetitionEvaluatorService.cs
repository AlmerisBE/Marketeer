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

        // New Logic: Filter out dumped listings if the specific defense strategy is selected
        if (anomalyReport != null && anomalyReport.IsAnomalyDetected && config.AnomalyStrategy == AnomalyDefenseStrategy.UndercutNormalMarket) {
            validCompetitors = validCompetitors.Where(l => l.Price > anomalyReport.CrashThresholdPrice).ToList();
        }

        // If there are no normal competitors left, we have no one to undercut normally. 
        // Returning false implicitly holds the price, which is the safest fallback.
        if (validCompetitors.Count == 0) return false;

        var lowestCompetitor = validCompetitors.First();

        if (listing.CurrentPrice < lowestCompetitor.Price) return false;
        if (listing.CurrentPrice == lowestCompetitor.Price) return false;

        uint targetPrice = lowestCompetitor.Price - 1;
        if (this.whitelistManager.IsWhitelisted(lowestCompetitor.RetainerName) && config.CompetitorWhitelistBehavior == WhitelistBehavior.MatchPrice) {
            targetPrice = lowestCompetitor.Price;
        }

        var suggestedAction = PricingAction.UpdatePrice;

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
            AverageMarketPrice = pricing.AverageSalePrice,
            CompetitorName = lowestCompetitor.RetainerName ?? "Unknown",
            CharacterName = characterName,
            SuggestedAction = suggestedAction,
            AnomalyData = anomalyReport
        };

        return true;
    }
}