using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using Marketeer.API.GameInterop.Contracts;
using Marketeer.API.InventoryTracking.Contracts;
using Marketeer.API.InventoryTracking.Models;
using Marketeer.Core.Configuration.Contracts;
using System;
using System.Collections.Generic;

namespace Marketeer.API.InventoryTracking.Services;

public class InventorySnapshotService : IInventorySnapshotService {
    private IInventoryService inventoryService;
    private IObjectTable objectTable;
    private IConfigurationService configService;

    public InventorySnapshotService(
        IInventoryService inventoryService,
        IObjectTable objectTable,
        IConfigurationService configService) {

        this.inventoryService = inventoryService;
        this.objectTable = objectTable;
        this.configService = configService;
    }

    public InventorySnapshot CreateSnapshot() {
        var localPlayer = this.objectTable.LocalPlayer;
        if (localPlayer == null || localPlayer.Name == null) {
            return new InventorySnapshot();
        }

        var snapshot = new InventorySnapshot {
            CharacterName = localPlayer.Name.TextValue,
            HomeWorldId = localPlayer.HomeWorld.RowId,
            Timestamp = DateTime.UtcNow,
            Items = new List<TrackedItem>()
        };

        InventoryType[] bags = {
            InventoryType.Inventory1, InventoryType.Inventory2,
            InventoryType.Inventory3, InventoryType.Inventory4
        };

        this.PopulateSnapshot(snapshot, bags);
        return snapshot;
    }

    public InventorySnapshot CreateRetainerSnapshot(ulong retainerId, string retainerName) {
        var snapshot = new InventorySnapshot {
            CharacterName = retainerName, // We reuse CharacterName to store the retainer's name
            HomeWorldId = 0, // Irrelevant for retainers
            Timestamp = DateTime.UtcNow,
            Items = new List<TrackedItem>()
        };

        // FFXIV natively uses RetainerPage1 through 7 for standard retainer bags
        InventoryType[] bags = {
            InventoryType.RetainerPage1, InventoryType.RetainerPage2, InventoryType.RetainerPage3,
            InventoryType.RetainerPage4, InventoryType.RetainerPage5, InventoryType.RetainerPage6,
            InventoryType.RetainerPage7
        };

        this.PopulateSnapshot(snapshot, bags);
        return snapshot;
    }

    private void PopulateSnapshot(InventorySnapshot snapshot, InventoryType[] bags) {
        foreach (var bag in bags) {
            var slots = this.inventoryService.GetInventorySlots(bag);
            foreach (var slot in slots) {
                if (slot.IsOccupied && slot.ItemId > 0) {
                    snapshot.Items.Add(new TrackedItem {
                        ItemId = slot.ItemId,
                        Quantity = slot.Quantity,
                        ContainerId = (uint)bag,
                        SlotIndex = (int)slot.SlotIndex
                    });
                }
            }
        }
    }

    public void SaveSnapshot(InventorySnapshot snapshot) {
        if (string.IsNullOrEmpty(snapshot.CharacterName)) {
            return;
        }

        var key = $"{snapshot.CharacterName}_{snapshot.HomeWorldId}";
        var config = this.configService.GetConfig();

        config.InventorySnapshots[key] = snapshot;
        this.configService.Save();
    }

    public InventorySnapshot? GetLatestSnapshot(string characterName, uint homeWorldId) {
        var key = $"{characterName}_{homeWorldId}";
        var config = this.configService.GetConfig();

        if (config.InventorySnapshots.TryGetValue(key, out var snapshot)) {
            return snapshot;
        }

        return null;
    }

    public void SaveRetainerSnapshot(ulong retainerId, InventorySnapshot snapshot) {
        if (retainerId == 0) {
            return;
        }

        var config = this.configService.GetConfig();
        // Fallback for older configurations that might not have this dictionary initialized
        if (config.RetainerInventorySnapshots == null) {
            config.RetainerInventorySnapshots = new Dictionary<ulong, InventorySnapshot>();
        }

        config.RetainerInventorySnapshots[retainerId] = snapshot;
        this.configService.Save();
    }

    public InventorySnapshot? GetLatestRetainerSnapshot(ulong retainerId) {
        var config = this.configService.GetConfig();
        if (config.RetainerInventorySnapshots != null && config.RetainerInventorySnapshots.TryGetValue(retainerId, out var snapshot)) {
            return snapshot;
        }

        return null;
    }
}