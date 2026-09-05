using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using Marketeer.API.GameInterop.Contracts;
using Marketeer.API.InventoryTracking.Contracts;
using Marketeer.API.InventoryTracking.Models;
using Marketeer.Core.Logging.Contracts;
using Marketeer.Core.RetainerAutomation.Contracts;
using Marketeer.UI.Localization.Contracts;
using System;
using System.Linq;

namespace Marketeer.Core.RetainerAutomation.Services;

public class ItemCancelAndSellService : IItemCancelAndSellService, IDisposable {
    private IRetainerUiInteractionService uiInteraction;
    private IInventoryService inventoryService;
    private ILocalizationService localization;
    private IFramework framework;
    private IInventorySnapshotService snapshotService;
    private IInventoryDiffService diffService;
    private ILoggerService logger;

    private bool isProcessing;
    private int stateMachineIndex;
    private DateTime nextActionAt;

    private uint targetItemId;
    private int sourceUiIndex;

    private InventorySnapshot? initialSnapshot;

    public ItemCancelAndSellService(
        IRetainerUiInteractionService uiInteraction,
        IInventoryService inventoryService,
        ILocalizationService localization,
        IFramework framework,
        IInventorySnapshotService snapshotService,
        IInventoryDiffService diffService,
        ILoggerService logger) {

        this.uiInteraction = uiInteraction;
        this.inventoryService = inventoryService;
        this.localization = localization;
        this.framework = framework;
        this.snapshotService = snapshotService;
        this.diffService = diffService;
        this.logger = logger;

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

        this.initialSnapshot = this.snapshotService.CreateSnapshot();

        this.isProcessing = true;
        this.stateMachineIndex = 0;
        this.nextActionAt = DateTime.Now;
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
                if (this.uiInteraction.IsAddonReady("SelectYesNo")) {
                    this.uiInteraction.ConfirmYesNo();
                    this.stateMachineIndex++; // Move to disappearance logging
                    this.nextActionAt = DateTime.Now.AddSeconds(1.0);
                }
                else if (this.uiInteraction.IsAddonReady("ContextMenu")) {
                    var returnText = this.localization.Translate("RetainerMenu_ReturnToInventory");
                    var menuIndex = this.uiInteraction.GetContextMenuItemIndex(returnText);
                    if (menuIndex == -1) {
                        menuIndex = 2;
                    }

                    this.uiInteraction.SelectContextMenuItem(menuIndex);

                    // Stay in stateMachineIndex 1 to catch SelectYesNo on the next tick
                    this.nextActionAt = DateTime.Now.AddSeconds(0.2);
                }
                break;

            case 2:
                this.LocateTransferredItemsAndLog();
                this.isProcessing = false;
                break;
        }
    }

    private void LocateTransferredItemsAndLog() {
        if (this.initialSnapshot == null) {
            return;
        }

        var newSnapshot = this.snapshotService.CreateSnapshot();
        var diff = this.diffService.Compare(this.initialSnapshot, newSnapshot);

        foreach (var added in diff.Added) {
            if (added.ItemId != this.targetItemId && added.ItemId != this.targetItemId + 1000000u) {
                continue;
            }

            this.logger.Debug($"[CancelAndSell] Item {added.ItemId} transferred to inventory -> Bag {added.ContainerId}, Slot {added.SlotIndex} | Quantity to sell: {added.Quantity}");
        }

        foreach (var changed in diff.QuantityChanged) {
            if (changed.ItemId != this.targetItemId && changed.ItemId != this.targetItemId + 1000000u) {
                continue;
            }

            if (changed.Difference > 0) {
                this.logger.Debug($"[CancelAndSell] Item {changed.ItemId} quantity increased in inventory -> Bag {changed.ContainerId}, Slot {changed.SlotIndex} | Quantity added to sell: {changed.Difference}");
            }
        }

        if (diff.Added.Count == 0 && diff.QuantityChanged.Count == 0) {
            this.logger.Warning($"[CancelAndSell] Could not locate item ID {this.targetItemId} in player inventory after cancellation.");
        }

        this.initialSnapshot = null;
    }

    public void Dispose() {
        this.framework.Update -= this.OnFrameworkUpdate;
    }
}