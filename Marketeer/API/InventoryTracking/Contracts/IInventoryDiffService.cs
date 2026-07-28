using Marketeer.API.InventoryTracking.Models;

namespace Marketeer.API.InventoryTracking.Contracts;

public interface IInventoryDiffService {
    InventoryDiff Compare(InventorySnapshot oldSnapshot, InventorySnapshot newSnapshot);
}