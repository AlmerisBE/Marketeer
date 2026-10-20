using Marketeer.API.Universalis.Models;
using Marketeer.Core.Configuration.Contracts;
using Marketeer.Core.Configuration.Models;
using Marketeer.Core.MarketPricing.Contracts;
using Marketeer.Core.MarketPricing.Models;
using Marketeer.Core.MarketStrategy.Contracts;
using Marketeer.Core.MarketStrategy.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Marketeer.Core.MarketStrategy.Services;

public class MarketAnomalyDetectorService : IMarketAnomalyDetector, IPriceCrashProtector {
    private IConfigurationService configService;

    public MarketAnomalyDetectorService(IConfigurationService configService) {
        this.configService = configService;
    }

    public void ApplyProtection(MarketItemPricing pricing, bool isHq, List<LowestPriceResult> validCompetitors, out bool holdPrice) {
        holdPrice = false;
        var report = this.EvaluateMarket(pricing, isHq);
        if (report == null || !report.IsAnomalyDetected) return;

        var config = this.configService.GetConfig();
        if (config.AnomalyStrategy == AnomalyDefenseStrategy.UndercutNormalMarket) {
            validCompetitors.RemoveAll(l => l.Price <= report.CrashThresholdPrice);
        }
        else if (config.AnomalyStrategy == AnomalyDefenseStrategy.HoldPrice || config.AnomalyStrategy == AnomalyDefenseStrategy.AlertAndPause) {
            holdPrice = true;
        }
    }

    public AnomalyReport EvaluateMarket(MarketItemPricing pricing, bool isHq) {
        var report = new AnomalyReport();

        if (pricing == null || pricing.Listings == null || pricing.Listings.Count == 0) return report;

        var config = this.configService.GetConfig();
        if (!config.EnableAnomalyProtection) return report;

        uint trueMarketValue = pricing.AverageSalePrice;
        if (trueMarketValue == 0) return report;

        report.TrueMarketValue = trueMarketValue;

        uint crashThresholdPrice = (uint)Math.Floor(trueMarketValue * config.AnomalyCrashThreshold);
        report.CrashThresholdPrice = crashThresholdPrice;

        var relevantListings = pricing.Listings.Where(l => l.IsHq == isHq).ToList();

        foreach (var listing in relevantListings) {
            if (listing.Price <= crashThresholdPrice) {
                report.IsAnomalyDetected = true;
                report.DumpedItemCount++;
                report.TotalBuyoutCost += listing.Price;
            }
            else break;
        }

        if (report.IsAnomalyDetected) {
            uint projectedRevenue = report.TrueMarketValue * (uint)report.DumpedItemCount;
            if (projectedRevenue > report.TotalBuyoutCost) {
                report.PotentialGrossProfit = projectedRevenue - report.TotalBuyoutCost;
            }
        }

        return report;
    }
}