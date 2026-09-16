using Marketeer.API.GameData.Models;
using System.Collections.Generic;

namespace Marketeer.API.GameData.Contracts;

public interface IMarketItemSearchProvider {
    IEnumerable<ItemSearchResult> SearchMarketableItems(string query, int limit = 20);
}