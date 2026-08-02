using Marketeer.API.MarketWatch.Models;
using System.Collections.Generic;

namespace Marketeer.API.MarketWatch.Contracts;

public interface IMarketWatchRepository {
    IReadOnlyList<WatchedItem> GetAllWatchedItems();
    void AddOrUpdateItem(WatchedItem item);
    void RemoveItem(uint itemId);
}