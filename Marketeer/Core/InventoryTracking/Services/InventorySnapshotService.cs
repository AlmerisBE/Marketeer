using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using Marketeer.API.Configuration.Contracts;
using Marketeer.API.GameInterop.Contracts;
using Marketeer.API.InventoryTracking.Contracts;
using Marketeer.API.InventoryTracking.Models;
using System;
using System.Collections.Generic;

namespace Marketeer.Core.InventoryTracking.Services;

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
            InventoryType.Inventory1,
            InventoryType.Inventory2,
            InventoryType.Inventory3,
            InventoryType.Inventory4
        };

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

        return snapshot;
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
}