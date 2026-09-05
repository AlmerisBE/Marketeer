using FFXIVClientStructs.FFXIV.Client.Game;
using Marketeer.API.GameInterop.Contracts;
using Marketeer.API.GameInterop.Models;
using Marketeer.Core.Logging.Contracts;
using Marketeer.Core.RetainerAutomation.Contracts;
using Marketeer.Core.RetainerAutomation.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Marketeer.Core.RetainerAutomation.Services;

public class CancelAndSellAutomationService : ICancelAndSellAutomationService, IRetainerTask {
    private IRetainerOrchestratorService orchestrator;
    private IRetainerUiInteractionService uiInteraction;
    private IInventoryService inventoryService;
    private ILoggerService logger;

    private uint targetItemId;
    private uint targetQuantity;
    private int sourceUiIndex;

    private Dictionary<InventoryType, List<InventorySlotInfo>> snapshotBefore;
    private Queue<TransferredItemStack> pendingSells;

    private int stateMachineIndex;
    private DateTime actionAvailableAt;

    public bool IsActive => this.orchestrator.IsActive;

    public CancelAndSellAutomationService(
        IRetainerOrchestratorService orchestrator,
        IRetainerUiInteractionService uiInteraction,
        IInventoryService inventoryService,
        ILoggerService logger) {

        this.orchestrator = orchestrator;
        this.uiInteraction = uiInteraction;
        this.inventoryService = inventoryService;
        this.logger = logger;
        this.snapshotBefore = new Dictionary<InventoryType, List<InventorySlotInfo>>();
        this.pendingSells = new Queue<TransferredItemStack>();
    }

    public void TriggerCancelAndSell(uint itemId, uint quantity, int uiIndex) {
        if (this.IsActive) {
            return;
        }

        this.targetItemId = itemId;
        this.targetQuantity = quantity;
        this.sourceUiIndex = uiIndex;
        this.stateMachineIndex = 0;
        this.pendingSells.Clear();

        this.TakeInventorySnapshot();

        this.orchestrator.StartOrchestration(new[] { "CURRENT" }, RetainerTargetMenu.MarketListings, this);
    }

    private void TakeInventorySnapshot() {
        this.snapshotBefore.Clear();
        for (var bag = InventoryType.RetainerPage1; bag <= InventoryType.RetainerPage7; bag++) {
            this.snapshotBefore[bag] = this.inventoryService.GetInventorySlots(bag).ToList();
        }
    }

    public void OnMenuOpened(string retainerName) { }

    public void OnMenuClosed(string retainerName) { }

    public void OnAbort() {
        this.pendingSells.Clear();
    }

    public bool OnTick() {
        if (DateTime.Now < this.actionAvailableAt) {
            return false;
        }

        switch (this.stateMachineIndex) {
            case 0:
                this.uiInteraction.SelectItemInSellList(this.sourceUiIndex);
                this.stateMachineIndex++;
                this.SetDelay(0.5);
                return false;

            case 1:
                if (this.uiInteraction.IsAddonReady("ContextMenu")) {
                    this.uiInteraction.SelectContextMenuItem(1);
                    this.stateMachineIndex++;
                    this.SetDelay(1.0);
                }
                return false;

            case 2:
                if (this.LocateTransferredItems()) {
                    this.logger.Debug($"[CancelAndSell] Successfully located {this.pendingSells.Count} stack(s) for item {this.targetItemId} to re-list.");
                    this.stateMachineIndex++;
                    this.SetDelay(0.5);
                }
                return false;

            case 3:
                if (this.pendingSells.Count == 0) {
                    return true; // Finish task when all stacks are processed
                }

                var stackToSell = this.pendingSells.Peek();
                this.logger.Debug($"[CancelAndSell] Triggering sell for {stackToSell.Quantity} units from Bag {stackToSell.Bag}, Slot {stackToSell.SlotIndex}.");

                // Native UI logic to right-click the bag slot and select "Sell" goes here.
                // For now, we simulate processing and pop the queue.

                this.pendingSells.Dequeue();
                this.SetDelay(1.0);
                return false;
        }

        return true;
    }

    private bool LocateTransferredItems() {
        var locations = new List<TransferredItemStack>();
        uint foundQuantity = 0;

        for (var bag = InventoryType.RetainerPage1; bag <= InventoryType.RetainerPage7; bag++) {
            var currentSlots = this.inventoryService.GetInventorySlots(bag);
            if (!this.snapshotBefore.TryGetValue(bag, out var oldSlots)) {
                continue;
            }

            foreach (var currentSlot in currentSlots) {
                if (!currentSlot.IsOccupied || currentSlot.ItemId != this.targetItemId) {
                    continue;
                }

                var oldSlot = oldSlots.FirstOrDefault(s => s.SlotIndex == currentSlot.SlotIndex);
                uint oldQuantity = (oldSlot != null && oldSlot.IsOccupied && oldSlot.ItemId == this.targetItemId) ? oldSlot.Quantity : 0;

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

        if (foundQuantity == this.targetQuantity) {
            this.pendingSells = new Queue<TransferredItemStack>(locations);
            return true;
        }

        return false;
    }

    private void SetDelay(double seconds) {
        this.actionAvailableAt = DateTime.Now.AddSeconds(seconds);
    }
}