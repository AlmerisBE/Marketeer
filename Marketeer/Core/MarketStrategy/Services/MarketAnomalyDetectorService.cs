using Marketeer.Core.Configuration.Contracts;
using Marketeer.Core.MarketPricing.Models;
using Marketeer.Core.MarketStrategy.Contracts;
using Marketeer.Core.MarketStrategy.Models;
using System;
using System.Linq;

namespace Marketeer.Core.MarketStrategy.Services;

public class MarketAnomalyDetectorService : IMarketAnomalyDetector {
    private IConfigurationService configService;

    public MarketAnomalyDetectorService(IConfigurationService configService) {
        this.configService = configService;
    }

    public AnomalyReport EvaluateMarket(MarketItemPricing pricing, bool isHq) {
        var report = new AnomalyReport();

        if (pricing == null || pricing.Listings == null || pricing.Listings.Count == 0) return report;

        var config = this.configService.GetConfig();
        if (!config.EnableAnomalyProtection) return report;

        // The True Market Value (TMV) is heavily anchored to Universalis's historical average sale price
        uint trueMarketValue = pricing.AverageSalePrice;

        // Fallback: If no average sale price is available (e.g., new item), we cannot reliably detect a crash based on history
        if (trueMarketValue == 0) return report;

        report.TrueMarketValue = trueMarketValue;

        uint crashThresholdPrice = (uint)Math.Floor(trueMarketValue * config.AnomalyCrashThreshold);

        var relevantListings = pricing.Listings.Where(l => l.IsHq == isHq).ToList();

        foreach (var listing in relevantListings) {
            if (listing.Price <= crashThresholdPrice) {
                report.IsAnomalyDetected = true;
                report.DumpedItemCount++;
                report.TotalBuyoutCost += listing.Price;
            }
            else break; // Listings are ordered by price, so once we exceed the threshold, we can stop
        }

        if (report.IsAnomalyDetected) {
            // Gross profit = (Selling all dumped items at TMV) - (Cost to buy them)
            uint projectedRevenue = report.TrueMarketValue * (uint)report.DumpedItemCount;

            if (projectedRevenue > report.TotalBuyoutCost) {
                report.PotentialGrossProfit = projectedRevenue - report.TotalBuyoutCost;
            }
        }

        return report;
    }
}