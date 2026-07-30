using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using Marketeer.API.GameInterop.Contracts;
using Marketeer.API.GameInterop.Models;
using Marketeer.API.Localization.Contracts;
using Marketeer.API.Logging.Contracts;
using Marketeer.API.RetainerAutomation.Contracts;
using Marketeer.API.RetainerAutomation.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Marketeer.Core.RetainerAutomation.Services;

public class ItemCancelAndSellService : IItemCancelAndSellService, IDisposable {
    private IRetainerUiInteractionService uiInteraction;
    private IInventoryService inventoryService;
    private ILocalizationService localization;
    private IFramework framework;
    private ILoggerService logger;

    private bool isProcessing;
    private int stateMachineIndex;
    private DateTime nextActionAt;

    private uint targetItemId;
    private int sourceUiIndex;

    private Dictionary<InventoryType, List<InventorySlotInfo>> snapshotBefore;

    public ItemCancelAndSellService(
        IRetainerUiInteractionService uiInteraction,
        IInventoryService inventoryService,
        ILocalizationService localization,
        IFramework framework,
        ILoggerService logger) {

        this.uiInteraction = uiInteraction;
        this.inventoryService = inventoryService;
        this.localization = localization;
        this.framework = framework;
        this.logger = logger;

        this.snapshotBefore = new Dictionary<InventoryType, List<InventorySlotInfo>>();
        this.isProcessing = false;

        this.framework.Update += this.OnFrameworkUpdate;
    }

    public void TriggerCancelAndSell(uint itemId) {
        if (this.isProcessing) {
            return;
        }

        var normalizedItemId = itemId > 1000000u ? itemId - 1000000u : itemId;
        var marketSlots = this.inventoryService.GetInventorySlots(InventoryType.RetainerMarket);
        var targetSlot = marketSlots.FirstOrDefault(s => s.IsOccupied && (s.ItemId == itemId || s.ItemId == normalizedItemId || s.ItemId == normalizedItemId + 1000000u));

        if (targetSlot == null) {
            this.logger.Warning($"[CancelAndSell] Could not find item ID {itemId} in retainer market inventory.");
            return;
        }

        this.targetItemId = normalizedItemId;
        this.sourceUiIndex = this.inventoryService.GetUiIndexForRetainerMarketItem((int)targetSlot.SlotIndex);

        if (this.sourceUiIndex == -1) {
            this.logger.Warning("[CancelAndSell] Could not resolve UI Index for targeted slot.");
            return;
        }

        this.TakeInventorySnapshot();

        this.isProcessing = true;
        this.stateMachineIndex = 0;
        this.nextActionAt = DateTime.Now;
    }

    private void TakeInventorySnapshot() {
        this.snapshotBefore.Clear();

        InventoryType[] playerBags = {
            InventoryType.Inventory1,
            InventoryType.Inventory2,
            InventoryType.Inventory3,
            InventoryType.Inventory4
        };

        foreach (var bag in playerBags) {
            this.snapshotBefore[bag] = this.inventoryService.GetInventorySlots(bag).ToList();
        }
    }

    private void OnFrameworkUpdate(IFramework fw) {
        if (!this.isProcessing || DateTime.Now < this.nextActionAt) {
            return;
        }

        switch (this.stateMachineIndex) {
            case 0:
                this.uiInteraction.SelectItemInSellList(this.sourceUiIndex);
                this.stateMachineIndex++;
                this.nextActionAt = DateTime.Now.AddSeconds(0.2);
                break;

            case 1:
                var returnText = this.localization.Translate("RetainerMenu_ReturnToInventory");
                var menuIndex = this.uiInteraction.GetContextMenuItemIndex(returnText);
                if (menuIndex == -1) {
                    menuIndex = 2;
                }

                this.uiInteraction.SelectContextMenuItem(menuIndex);
                this.stateMachineIndex++;
                this.nextActionAt = DateTime.Now.AddSeconds(1.0);
                break;

            case 2:
                this.LocateTransferredItemsAndLog();
                this.isProcessing = false;
                break;
        }
    }

    private void LocateTransferredItemsAndLog() {
        var locations = new List<TransferredItemStack>();
        uint foundQuantity = 0;

        InventoryType[] playerBags = {
            InventoryType.Inventory1,
            InventoryType.Inventory2,
            InventoryType.Inventory3,
            InventoryType.Inventory4
        };

        foreach (var bag in playerBags) {
            var currentSlots = this.inventoryService.GetInventorySlots(bag);
            if (!this.snapshotBefore.TryGetValue(bag, out var oldSlots)) {
                continue;
            }

            foreach (var currentSlot in currentSlots) {
                if (!currentSlot.IsOccupied) {
                    continue;
                }

                var normalizedCurrentId = currentSlot.ItemId > 1000000u ? currentSlot.ItemId - 1000000u : currentSlot.ItemId;
                if (normalizedCurrentId != this.targetItemId) {
                    continue;
                }

                var oldSlot = oldSlots.FirstOrDefault(s => s.SlotIndex == currentSlot.SlotIndex);
                uint oldQuantity = 0;

                if (oldSlot != null && oldSlot.IsOccupied) {
                    var normalizedOldId = oldSlot.ItemId > 1000000u ? oldSlot.ItemId - 1000000u : oldSlot.ItemId;
                    if (normalizedOldId == this.targetItemId) {
                        oldQuantity = oldSlot.Quantity;
                    }
                }

                if (currentSlot.Quantity > oldQuantity) {
                    uint addedQuantity = currentSlot.Quantity - oldQuantity;
                    locations.Add(new TransferredItemStack {
                        Bag = bag,
                        SlotIndex = currentSlot.SlotIndex,
                        Quantity = addedQuantity
                    });

                    foundQuantity += addedQuantity;
                }
            }
        }

        if (locations.Count > 0) {
            this.logger.Info($"[CancelAndSell] Cancellation successful. Located {locations.Count} stack(s) of item ID {this.targetItemId}.");
            foreach (var loc in locations) {
                this.logger.Info($" -> Target: Bag {loc.Bag}, Slot {loc.SlotIndex} | Quantity: {loc.Quantity} (Would be sold in real conditions)");
            }
        }
        else {
            this.logger.Warning($"[CancelAndSell] Could not locate item ID {this.targetItemId} in player inventory after cancellation.");
        }
    }

    public void Dispose() {
        this.framework.Update -= this.OnFrameworkUpdate;
    }
}