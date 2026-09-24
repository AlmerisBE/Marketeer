using Marketeer.Core.Logging.Contracts;
using Marketeer.Core.MarketPricing.Contracts;
using Marketeer.Core.MarketWatch.Contracts;
using Marketeer.Core.MarketWatch.Models;
using Marketeer.Core.SalesHistory.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Marketeer.Core.MarketWatch.Services;

public class MarketWatchAnalysisService : IMarketWatchAnalysisService {
    private IMarketWatchRepository repository;
    private IMarketPriceCacheService priceProvider;
    private IMarketWatchPlayerContext playerContext;
    private IItemResolverService itemResolver;
    private ILoggerService logger;

    public MarketWatchAnalysisService(
        IMarketWatchRepository repository,
        IMarketPriceCacheService priceProvider,
        IMarketWatchPlayerContext playerContext,
        IItemResolverService itemResolver,
        ILoggerService logger) {

        this.repository = repository;
        this.priceProvider = priceProvider;
        this.playerContext = playerContext;
        this.itemResolver = itemResolver;
        this.logger = logger;
    }

    public async Task<IReadOnlyList<MarketWatchAlert>> AnalyzeMarketAsync(bool bypassCache = false) {
        var alerts = new List<MarketWatchAlert>();

        if (!this.playerContext.IsPlayerAvailable()) return alerts;

        var worldId = this.playerContext.GetCurrentWorldId();
        var eligibleItems = this.repository.GetAllWatchedItems().Where(i => i.IsEligibleForPolling()).ToList();

        if (eligibleItems.Count == 0) return alerts;

        try {
            // Map WatchedItems correctly to exact FFXIV IDs (including HQ offsets) for specific querying
            var itemIds = eligibleItems.Select(i => i.IsHighQuality ? i.ItemId + 1000000u : i.ItemId).Distinct().ToList();
            var pricings = await this.priceProvider.GetPricingsAsync(itemIds, worldId, bypassCache);

            foreach (var item in eligibleItems) {
                var queryId = item.IsHighQuality ? item.ItemId + 1000000u : item.ItemId;
                var pricing = pricings.FirstOrDefault(p => p.ItemId == queryId);

                if (pricing == null || pricing.Listings.Count == 0) continue;

                // Since pricing already strictly filters by HQ status internally based on queryId, we just take the absolute lowest.
                var effectiveMarketData = pricing.Listings.OrderBy(p => p.Price).FirstOrDefault();
                if (effectiveMarketData == null) continue;

                var itemName = this.itemResolver.ResolveItemName(item.ItemId);

                if (item.IsBuyWatchEnabled && item.TargetBuyPrice.HasValue && effectiveMarketData.Price < item.TargetBuyPrice.Value) {
                    alerts.Add(new MarketWatchAlert {
                        ItemId = item.ItemId,
                        ItemName = itemName,
                        IsHighQuality = effectiveMarketData.IsHq,
                        AlertType = MarketWatchAlertType.BuyTargetReached,
                        TargetPrice = item.TargetBuyPrice.Value,
                        CurrentPrice = effectiveMarketData.Price,
                        RetainerName = effectiveMarketData.RetainerName
                    });
                }

                if (item.IsSellWatchEnabled && item.TargetSellPrice.HasValue && effectiveMarketData.Price >= item.TargetSellPrice.Value) {
                    alerts.Add(new MarketWatchAlert {
                        ItemId = item.ItemId,
                        ItemName = itemName,
                        IsHighQuality = item.IsHighQuality,
                        AlertType = MarketWatchAlertType.SellTargetReached,
                        TargetPrice = item.TargetSellPrice.Value,
                        CurrentPrice = effectiveMarketData.Price,
                        RetainerName = effectiveMarketData.RetainerName
                    });
                }
            }
        }
        catch (Exception ex) {
            this.logger.Error(ex, "Failed to analyze market prices for watched items.");
        }

        return alerts;
    }
}