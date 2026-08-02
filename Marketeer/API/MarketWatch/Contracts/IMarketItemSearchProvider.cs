using System.Collections.Generic;

namespace Marketeer.API.MarketWatch.Contracts;

public interface IMarketItemSearchProvider {
    IEnumerable<ItemSearchResult> SearchMarketableItems(string query, int limit = 20);
}

public class ItemSearchResult {
    public uint ItemId { get; set; }
    public string Name { get; set; } = string.Empty;
}