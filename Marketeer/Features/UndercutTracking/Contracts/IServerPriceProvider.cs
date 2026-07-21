using Marketeer.Features.UndercutTracking.Models;
using System.Threading.Tasks;

namespace Marketeer.Features.UndercutTracking.Contracts;

public interface IServerPriceProvider {
    Task<LowestPriceResult?> GetLowestPriceAsync(uint itemId, uint worldId);
}