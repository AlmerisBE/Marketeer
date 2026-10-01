using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using Marketeer.API.GameInterop.Contracts;
using Marketeer.API.InventoryTracking.Models;
using Marketeer.Core.CharacterManagement.Contracts;
using Marketeer.Core.CharacterManagement.Models;
using Marketeer.Core.Configuration.Contracts;
using Marketeer.Core.Financials.Models;
using Marketeer.Core.Logging.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Marketeer.Core.CharacterManagement.Services;

public class RetainerTrackerService : IRetainerTrackerService, IDisposable {
    private IObjectTable objectTable;
    private IConfigurationService configService;
    private IRetainerProvider retainerProvider;
    private ICharacterTrackerService characterTrackerService;
    private IGameEventService gameEventService;
    private ILoggerService logger;
    private IInventoryService inventoryService;

    public RetainerTrackerService(
        IObjectTable objectTable,
        IConfigurationService configService,
        IRetainerProvider retainerProvider,
        ICharacterTrackerService characterTrackerService,
        IGameEventService gameEventService,
        ILoggerService logger,
        IInventoryService inventoryService) {

        this.objectTable = objectTable;
        this.configService = configService;
        this.retainerProvider = retainerProvider;
        this.characterTrackerService = characterTrackerService;
        this.gameEventService = gameEventService;
        this.logger = logger;
        this.inventoryService = inventoryService;

        this.gameEventService.RetainerBellOpened += this.RecordRetainers;
        this.gameEventService.RetainerSellListUpdated += this.RecordRetainers;
    }

    public IReadOnlyList<TrackedRetainer> GetRetainersForCharacter(string characterName, uint homeWorldId) {
        var config = this.configService.GetConfig();
        var storageKey = $"{characterName}_{homeWorldId}";

        if (!config.FinancialRecords.TryGetValue(storageKey, out var charData)) {
            return new List<TrackedRetainer>();
        }

        return charData.Retainers.Values.Select(r => new TrackedRetainer {
            RetainerId = r.RetainerId,
            Name = r.Name,
            Gil = (uint)r.GilHeld,
            MarketItemCount = (uint)r.MarketListings.Count,
            AssociatedCharacterName = characterName,
            AssociatedHomeWorldId = homeWorldId
        }).ToList();
    }

    public void RecordRetainers() {
        var localPlayer = this.objectTable.LocalPlayer;
        if (localPlayer == null || localPlayer.Name == null) {
            return;
        }

        var activeRetainers = this.retainerProvider.GetActiveRetainers();
        if (activeRetainers.Count == 0) {
            return;
        }

        var config = this.configService.GetConfig();
        var storageKey = $"{localPlayer.Name.TextValue}_{localPlayer.HomeWorld.RowId}";

        lock (config) {
            if (!config.FinancialRecords.TryGetValue(storageKey, out var charData)) {
                return;
            }

            bool isModified = false;

            foreach (var retainer in activeRetainers) {
                if (!charData.Retainers.TryGetValue(retainer.RetainerId, out var existing)) {
                    existing = new RetainerFinancialData {
                        RetainerId = retainer.RetainerId,
                        Name = retainer.Name,
                        GilHeld = retainer.Gil
                    };
                    charData.Retainers[retainer.RetainerId] = existing;
                    isModified = true;
                }
                else {
                    if (existing.Name != retainer.Name || existing.GilHeld != retainer.Gil) {
                        existing.Name = retainer.Name;
                        existing.GilHeld = retainer.Gil;
                        isModified = true;
                    }
                }

                if (this.UpdateRetainerCrystals(retainer.RetainerId)) {
                    isModified = true;
                }
            }

            if (isModified) {
                this.configService.Save();
            }
        }
    }

    private bool UpdateRetainerCrystals(ulong retainerId) {
        var config = this.configService.GetConfig();
        if (!config.RetainerInventorySnapshots.TryGetValue(retainerId, out var snapshot)) {
            snapshot = new InventorySnapshot { Timestamp = DateTime.UtcNow, Items = new List<TrackedItem>() };
            config.RetainerInventorySnapshots[retainerId] = snapshot;
        }

        bool changed = false;
        var existingCrystals = snapshot.Items.Where(i => i.ItemId is >= 2 and <= 19).ToList();

        var crystalSlots = this.inventoryService.GetInventorySlots(InventoryType.RetainerCrystals);
        var newCrystalItems = new List<TrackedItem>();

        if (crystalSlots != null) {
            foreach (var slot in crystalSlots.Where(s => s.IsOccupied && s.Quantity > 0)) {
                newCrystalItems.Add(new TrackedItem {
                    ContainerId = (int)InventoryType.RetainerCrystals,
                    SlotIndex = (int)slot.SlotIndex,
                    ItemId = slot.ItemId,
                    Quantity = slot.Quantity
                });
            }
        }

        if (existingCrystals.Count != newCrystalItems.Count) changed = true;
        else {
            foreach (var newItem in newCrystalItems) {
                var match = existingCrystals.FirstOrDefault(e => e.ItemId == newItem.ItemId && e.Quantity == newItem.Quantity);
                if (match == null) {
                    changed = true;
                    break;
                }
            }
        }

        if (changed) {
            snapshot.Items.RemoveAll(i => i.ItemId is >= 2 and <= 19);
            snapshot.Items.AddRange(newCrystalItems);
            snapshot.Timestamp = DateTime.UtcNow;
        }

        return changed;
    }

    public void Dispose() {
        this.gameEventService.RetainerBellOpened -= this.RecordRetainers;
        this.gameEventService.RetainerSellListUpdated -= this.RecordRetainers;
    }
}