using Marketeer.API.Universalis.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Marketeer.API.Universalis.Contracts;

public interface IUniversalisClient {
    Task<IReadOnlyList<UniversalisItemData>> FetchDataAsync(IEnumerable<uint> baseItemIds, uint worldId);
}