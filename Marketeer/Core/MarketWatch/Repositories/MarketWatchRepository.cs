using Marketeer.API.MarketWatch.Contracts;
using Marketeer.API.MarketWatch.Models;
using System.Collections.Generic;
using System.Linq;

namespace Marketeer.Core.MarketWatch.Repositories;

public class MarketWatchRepository : IMarketWatchRepository {
    private readonly Dictionary<uint, WatchedItem> watchedItems;

    public MarketWatchRepository() {
        this.watchedItems = new Dictionary<uint, WatchedItem>();
    }

    public IReadOnlyList<WatchedItem> GetAllWatchedItems() {
        return this.watchedItems.Values.ToList().AsReadOnly();
    }

    public void AddOrUpdateItem(WatchedItem item) {
        if (item == null) {
            return;
        }

        this.watchedItems[item.ItemId] = item;
    }

    public void RemoveItem(uint itemId) {
        if (this.watchedItems.ContainsKey(itemId)) {
            this.watchedItems.Remove(itemId);
        }
    }
}