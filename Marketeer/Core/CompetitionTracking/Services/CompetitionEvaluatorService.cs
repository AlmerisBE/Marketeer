using Marketeer.Core.CompetitionTracking.Contracts;
using Marketeer.Core.CompetitionTracking.Models;
using Marketeer.Core.MarketPricing.Contracts;
using Marketeer.Core.MarketPricing.Models;
using Marketeer.Core.MarketStrategy.Contracts;
using Marketeer.UI.CompetitionTracking.Models;

namespace Marketeer.Core.CompetitionTracking.Services;

public class CompetitionEvaluatorService : ICompetitionEvaluatorService {
    private IPriceCalculationService priceCalculationService;
    private IMarketAnomalyDetector anomalyDetector;

    public CompetitionEvaluatorService(
        IPriceCalculationService priceCalculationService,
        IMarketAnomalyDetector anomalyDetector) {

        this.priceCalculationService = priceCalculationService;
        this.anomalyDetector = anomalyDetector;
    }

    public bool TryEvaluateListing(RetainerListing listing, MarketItemPricing pricing, string characterName, uint homeWorldId, string resolvedItemName, out UndercutItem? undercutResult) {
        undercutResult = null;

        if (pricing.Listings == null || pricing.Listings.Count == 0) return false;

        // Single Source of Truth for pricing logic
        var calcResult = this.priceCalculationService.CalculateTargetPrice(listing.ItemId, listing.CurrentPrice, pricing, listing.IsHq);

        // We only care if the action requires a change, OR if it's kept because of loss prevention or crash protection
        if (calcResult.Action == PricingAction.KeepPrice && calcResult.Reason != PricingReason.VendorMinimumReached && calcResult.Reason != PricingReason.MarketCrashProtection) {
            return false;
        }

        if (calcResult.Reason == PricingReason.AlreadyLowest || calcResult.Reason == PricingReason.NoCompetition || calcResult.Reason == PricingReason.StorageProtection) {
            return false;
        }

        var anomalyReport = this.anomalyDetector.EvaluateMarket(pricing, listing.IsHq);

        undercutResult = new UndercutItem {
            SlotIndex = listing.SlotIndex,
            ItemId = listing.ItemId,
            ItemName = resolvedItemName,
            Quantity = listing.Quantity,
            RetainerName = listing.RetainerName,
            Price = listing.CurrentPrice,
            OurPrice = listing.CurrentPrice,
            ServerCheapestPrice = calcResult.CompetitorPrice > 0 ? calcResult.CompetitorPrice : calcResult.CalculatedPrice,
            TargetPrice = calcResult.CalculatedPrice,
            AverageMarketPrice = pricing.AverageSalePrice,
            CompetitorName = string.IsNullOrEmpty(calcResult.ContextData) ? "Unknown" : calcResult.ContextData,
            CharacterName = characterName,
            HomeWorldId = homeWorldId,
            SuggestedAction = calcResult.Action,
            AnomalyData = anomalyReport
        };

        return true;
    }
}