using Marketeer.API.Universalis.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Marketeer.API.Universalis.Contracts;

public interface IUniversalisClient {
    Task<IReadOnlyList<LowestPriceResult>> FetchPricesAsync(IEnumerable<uint> itemIds, uint worldId);
}