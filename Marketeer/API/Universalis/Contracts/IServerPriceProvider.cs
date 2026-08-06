using Marketeer.API.Universalis.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Marketeer.API.Universalis.Contracts;

public interface IServerPriceProvider {
    Task<LowestPriceResult?> GetLowestPriceAsync(uint itemId, uint worldId, bool bypassCache = false);
    Task<IReadOnlyList<LowestPriceResult>> GetLowestPricesAsync(IEnumerable<uint> itemIds, uint worldId, bool bypassCache = false);
}