using Marketeer.API.Configuration.Contracts;
using Marketeer.API.MarketWatch.Contracts;
using Marketeer.API.MarketWatch.Models;
using System.Collections.Generic;
using System.Linq;

namespace Marketeer.Core.MarketWatch.Repositories;

public class MarketWatchRepository : IMarketWatchRepository {
    private IConfigurationService configService;

    public MarketWatchRepository(IConfigurationService configService) {
        this.configService = configService;

        if (this.configService.GetConfig().WatchedItems == null) {
            this.configService.GetConfig().WatchedItems = new Dictionary<string, WatchedItem>();
        }
    }

    public IReadOnlyList<WatchedItem> GetAllWatchedItems() {
        return this.configService.GetConfig().WatchedItems.Values.ToList().AsReadOnly();
    }

    public void AddOrUpdateItem(WatchedItem item) {
        if (item == null) {
            return;
        }

        this.configService.GetConfig().WatchedItems[item.Key] = item;
        this.configService.Save();
    }

    public void RemoveItem(uint itemId, bool isHighQuality) {
        var key = $"{itemId}_{(isHighQuality ? "HQ" : "NQ")}";
        if (this.configService.GetConfig().WatchedItems.Remove(key)) {
            this.configService.Save();
        }
    }
}