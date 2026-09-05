using Marketeer.API.Universalis.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Marketeer.API.Universalis.Contracts;

public interface IServerPriceProvider {
    event Action<uint, IEnumerable<uint>>? PricesUpdated;

    Task<LowestPriceResult?> GetLowestPriceAsync(uint itemId, uint worldId, bool bypassCache = false);
    Task<IReadOnlyList<LowestPriceResult>> GetLowestPricesAsync(IEnumerable<uint> itemIds, uint worldId, bool bypassCache = false);

    Task ForceRefreshAsync(IEnumerable<uint> itemIds, uint worldId);

    void UpdateLocalCache(uint itemId, uint worldId, IReadOnlyList<LowestPriceResult> prices);
}