using Marketeer.API.Universalis.Models;
using Marketeer.Core.MarketPricing.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Marketeer.Core.MarketPricing.Contracts;

public interface IMarketPriceCacheService {
    event Action<uint, IEnumerable<uint>>? PricesUpdated;

    Task<MarketItemPricing?> GetPricingAsync(uint itemId, uint worldId, bool bypassCache = false);
    Task<IReadOnlyList<MarketItemPricing>> GetPricingsAsync(IEnumerable<uint> itemIds, uint worldId, bool bypassCache = false);
    Task ForceRefreshAsync(IEnumerable<uint> itemIds, uint worldId);

    void UpdateLocalPrices(uint itemId, uint worldId, IReadOnlyList<LowestPriceResult> prices);
}