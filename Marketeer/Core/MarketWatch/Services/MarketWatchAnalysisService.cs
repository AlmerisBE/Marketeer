using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin.Services;
using Marketeer.API.Logging.Contracts;
using Marketeer.API.MarketWatch.Contracts;
using Marketeer.API.MarketWatch.Models;
using Marketeer.API.SalesHistory.Contracts;
using Marketeer.API.Universalis.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Marketeer.Core.MarketWatch.Services;

public class MarketWatchAnalysisService : IMarketWatchAnalysisService {
    private IMarketWatchRepository repository;
    private IServerPriceProvider priceProvider;
    private IObjectTable objectTable;
    private IItemResolverService itemResolver;
    private ILoggerService logger;

    public MarketWatchAnalysisService(
        IMarketWatchRepository repository,
        IServerPriceProvider priceProvider,
        IObjectTable objectTable,
        IItemResolverService itemResolver,
        ILoggerService logger) {

        this.repository = repository;
        this.priceProvider = priceProvider;
        this.objectTable = objectTable;
        this.itemResolver = itemResolver;
        this.logger = logger;
    }

    public async Task<IReadOnlyList<MarketWatchAlert>> AnalyzeMarketAsync() {
        var alerts = new List<MarketWatchAlert>();

        if (this.objectTable.Length == 0) {
            return alerts;
        }

        var localPlayer = this.objectTable[0] as IPlayerCharacter;
        if (localPlayer == null) {
            return alerts;
        }

        var worldId = localPlayer.CurrentWorld.RowId;
        var eligibleItems = this.repository.GetAllWatchedItems()
            .Where(i => i.IsEligibleForPolling())
            .ToList();

        if (eligibleItems.Count == 0) {
            return alerts;
        }

        var itemIds = eligibleItems.Select(i => i.ItemId).Distinct().ToList();

        try {
            var lowestPrices = await this.priceProvider.GetLowestPricesAsync(itemIds, worldId);

            foreach (var item in eligibleItems) {
                var marketData = lowestPrices.FirstOrDefault(p => p.ItemId == item.ItemId);
                if (marketData == null) {
                    continue;
                }

                var itemName = this.itemResolver.ResolveItemName(item.ItemId);

                if (item.IsBuyWatchEnabled && item.TargetBuyPrice.HasValue && marketData.Price < item.TargetBuyPrice.Value) {
                    alerts.Add(new MarketWatchAlert {
                        ItemId = item.ItemId,
                        ItemName = itemName,
                        AlertType = MarketWatchAlertType.BuyTargetReached,
                        TargetPrice = item.TargetBuyPrice.Value,
                        CurrentPrice = marketData.Price,
                        RetainerName = marketData.RetainerName
                    });
                }

                if (item.IsSellWatchEnabled && item.TargetSellPrice.HasValue && marketData.Price >= item.TargetSellPrice.Value) {
                    alerts.Add(new MarketWatchAlert {
                        ItemId = item.ItemId,
                        ItemName = itemName,
                        AlertType = MarketWatchAlertType.SellTargetReached,
                        TargetPrice = item.TargetSellPrice.Value,
                        CurrentPrice = marketData.Price,
                        RetainerName = marketData.RetainerName
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