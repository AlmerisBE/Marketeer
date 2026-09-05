using Marketeer.Core.MarketWatch.Models;
using System.Collections.Generic;

namespace Marketeer.Core.MarketWatch.Contracts;

public interface IMarketWatchRepository {
    IReadOnlyList<WatchedItem> GetAllWatchedItems();
    void AddOrUpdateItem(WatchedItem item);
    void RemoveItem(uint itemId, bool isHighQuality);
}