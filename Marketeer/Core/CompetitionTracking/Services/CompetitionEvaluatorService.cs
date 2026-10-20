using Marketeer.API.Universalis.Models;
using Marketeer.Core.CompetitionTracking.Contracts;
using Marketeer.Core.CompetitionTracking.Models;
using Marketeer.Core.Configuration.Contracts;
using Marketeer.Core.Configuration.Models;
using Marketeer.Core.MarketPricing.Models;
using Marketeer.Core.MarketStrategy.Contracts;
using Marketeer.UI.CompetitionTracking.Models;
using System;
using System.Linq;

namespace Marketeer.Core.CompetitionTracking.Services;

public class CompetitionEvaluatorService : ICompetitionEvaluatorService {
    private IConfigurationService configService;
    private IWhitelistManagerService whitelistManager;
    private IMarketAnomalyDetector anomalyDetector;
    private ICompetitionRuleProvider ruleProvider;

    public CompetitionEvaluatorService(
        IConfigurationService configService,
        IWhitelistManagerService whitelistManager,
        IMarketAnomalyDetector anomalyDetector,
        ICompetitionRuleProvider ruleProvider) {

        this.configService = configService;
        this.whitelistManager = whitelistManager;
        this.anomalyDetector = anomalyDetector;
        this.ruleProvider = ruleProvider;
    }

    public bool TryEvaluateListing(RetainerListing listing, MarketItemPricing pricing, string characterName, uint homeWorldId, string resolvedItemName, out UndercutItem? undercutResult) {
        undercutResult = null;

        if (pricing.Listings == null || !pricing.Listings.Any()) return false;

        var config = this.configService.GetConfig();
        var rule = this.ruleProvider.GetRuleForItem(listing.ItemId);
        var anomalyReport = this.anomalyDetector.EvaluateMarket(pricing, listing.IsHq);

        var validCompetitors = pricing.Listings
            .Where(l => !(this.whitelistManager.IsWhitelisted(l.RetainerName) && config.CompetitorWhitelistBehavior == WhitelistBehavior.Ignore))
            .OrderBy(l => l.Price)
            .ToList();

        if (anomalyReport != null && anomalyReport.IsAnomalyDetected && config.AnomalyStrategy == AnomalyDefenseStrategy.UndercutNormalMarket) {
            validCompetitors = validCompetitors.Where(l => l.Price > anomalyReport.CrashThresholdPrice).ToList();
        }

        if (validCompetitors.Count == 0) return false;

        bool ignoreHq = rule.IgnoreHq ?? false;
        double premium = rule.HqPremiumPercentage ?? 0.0;
        var undercutMode = rule.UndercutMode ?? UndercutMode.Absolute;
        uint undercutAmount = rule.UndercutAmount ?? 1;
        double undercutPct = rule.UndercutRelativePercentage ?? 1.0;

        LowestPriceResult? lowestCompetitor = null;

        if (ignoreHq) {
            lowestCompetitor = validCompetitors.FirstOrDefault();
        }
        else {
            if (listing.IsHq) {
                lowestCompetitor = validCompetitors.FirstOrDefault(l => l.IsHq);
                if (lowestCompetitor == null) {
                    lowestCompetitor = validCompetitors.FirstOrDefault(l => !l.IsHq);
                }
            }
            else {
                lowestCompetitor = validCompetitors.FirstOrDefault();
            }
        }

        if (lowestCompetitor == null) return false;

        uint targetPrice = listing.CurrentPrice;

        if (!ignoreHq && listing.IsHq && !lowestCompetitor.IsHq) {
            targetPrice = (uint)Math.Floor(lowestCompetitor.Price * (1.0 + (premium / 100.0)));
        }
        else {
            if (this.whitelistManager.IsWhitelisted(lowestCompetitor.RetainerName) && config.CompetitorWhitelistBehavior == WhitelistBehavior.MatchPrice) {
                targetPrice = lowestCompetitor.Price;
            }
            else {
                if (undercutMode == UndercutMode.Absolute) {
                    targetPrice = (uint)Math.Max(1, (int)lowestCompetitor.Price - (int)undercutAmount);
                }
                else {
                    targetPrice = (uint)Math.Max(1, lowestCompetitor.Price - (lowestCompetitor.Price * (undercutPct / 100.0)));
                }
            }
        }

        if (targetPrice == listing.CurrentPrice) return false;

        var suggestedAction = targetPrice > listing.CurrentPrice ? PricingAction.IncreasePrice : PricingAction.DecreasePrice;

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
            HomeWorldId = homeWorldId,
            SuggestedAction = suggestedAction,
            AnomalyData = anomalyReport
        };

        return true;
    }
}