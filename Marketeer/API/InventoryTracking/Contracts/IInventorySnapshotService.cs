using Marketeer.API.InventoryTracking.Models;

namespace Marketeer.API.InventoryTracking.Contracts;

public interface IInventorySnapshotService {
    InventorySnapshot CreateSnapshot();
    void SaveSnapshot(InventorySnapshot snapshot);
    InventorySnapshot? GetLatestSnapshot(string characterName, uint homeWorldId);

    InventorySnapshot CreateRetainerSnapshot(ulong retainerId, string retainerName);
    void SaveRetainerSnapshot(ulong retainerId, InventorySnapshot snapshot);
    InventorySnapshot? GetLatestRetainerSnapshot(ulong retainerId);
}