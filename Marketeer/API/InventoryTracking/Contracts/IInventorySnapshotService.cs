using Marketeer.API.InventoryTracking.Models;

namespace Marketeer.API.InventoryTracking.Contracts;

public interface IInventorySnapshotService {
    InventorySnapshot CreateSnapshot();
    void SaveSnapshot(InventorySnapshot snapshot);
    InventorySnapshot? GetLatestSnapshot(string characterName, uint homeWorldId);
}