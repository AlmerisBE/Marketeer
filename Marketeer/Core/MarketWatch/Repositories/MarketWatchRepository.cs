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

        // Guard against null collections when loading old configuration files
        if (this.configService.GetConfig().WatchedItems == null) {
            this.configService.GetConfig().WatchedItems = new Dictionary<uint, WatchedItem>();
        }
    }

    public IReadOnlyList<WatchedItem> GetAllWatchedItems() {
        return this.configService.GetConfig().WatchedItems.Values.ToList().AsReadOnly();
    }

    public void AddOrUpdateItem(WatchedItem item) {
        if (item == null) {
            return;
        }

        this.configService.GetConfig().WatchedItems[item.ItemId] = item;
        this.configService.Save();
    }

    public void RemoveItem(uint itemId) {
        if (this.configService.GetConfig().WatchedItems.Remove(itemId)) {
            this.configService.Save();
        }
    }
}