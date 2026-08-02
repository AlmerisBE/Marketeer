using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using Marketeer.API.GameInterop.Contracts;
using Marketeer.API.InventoryTracking.Contracts;
using Marketeer.API.InventoryTracking.Models;
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
    private IInventorySnapshotService snapshotService;
    private IInventoryDiffService diffService;
    private ILoggerService logger;

    private int stateMachineIndex;
    private DateTime nextActionAt;
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

        this.stateMachineIndex = 0;

        this.framework.Update += this.OnFrameworkUpdate;
    }

    public void TriggerCancelAndSell(object? menuTarget) {
        if (this.stateMachineIndex != 0) {
            return;
        }

        int uiIndex = -1;

        if (menuTarget != null) {
            var prop = menuTarget.GetType().GetProperty("TargetIndex", BindingFlags.Public | BindingFlags.Instance);
            if (prop != null) {
                var val = prop.GetValue(menuTarget);
                if (val is uint uIndex) {
                    uiIndex = this.inventoryService.GetUiIndexForRetainerMarketItem((int)uIndex);
                }
                else if (val is int iIndex) {
                    uiIndex = this.inventoryService.GetUiIndexForRetainerMarketItem(iIndex);
                }
            }
        }
    }

        // Take the initial inventory snapshot right before starting the cancellation process
        this.initialSnapshot = this.snapshotService.CreateSnapshot();

        if (uiIndex != -1) {
            this.uiInteraction.SelectItemInSellList(uiIndex);
            this.stateMachineIndex = 1;
            this.nextActionAt = DateTime.Now.AddSeconds(0.2);
        }
        else {
            var returnText = this.localization.Translate("RetainerMenu_ReturnToInventory");
            var menuIndex = this.uiInteraction.GetContextMenuItemIndex(returnText);

            if (menuIndex == -1) {
                menuIndex = 2;
            }

            this.uiInteraction.SelectContextMenuItem(menuIndex);
            this.stateMachineIndex = 2;
            this.nextActionAt = DateTime.Now.AddSeconds(1.0);
        }
    }

    private void OnFrameworkUpdate(IFramework fw) {
        if (this.stateMachineIndex == 0 || DateTime.Now < this.nextActionAt) {
            return;
        }

        if (this.stateMachineIndex == 1) {
            var returnText = this.localization.Translate("RetainerMenu_ReturnToInventory");
            var menuIndex = this.uiInteraction.GetContextMenuItemIndex(returnText);

            if (menuIndex == -1) {
                menuIndex = 2;
            }

            this.uiInteraction.SelectContextMenuItem(menuIndex);
            this.stateMachineIndex = 2;
            this.nextActionAt = DateTime.Now.AddSeconds(1.0);
        }
        else if (this.stateMachineIndex == 2) {
            var newSnapshot = this.snapshotService.CreateSnapshot();

            if (this.initialSnapshot != null) {
                var diff = this.diffService.Compare(this.initialSnapshot, newSnapshot);

                foreach (var added in diff.Added) {
                    this.logger.Info($"[CancelAndSell] Item {added.ItemId} transferred to inventory -> Bag {added.ContainerId}, Slot {added.SlotIndex} | Quantity to sell: {added.Quantity}");
                }

                foreach (var changed in diff.QuantityChanged) {
                    if (changed.Difference > 0) {
                        this.logger.Info($"[CancelAndSell] Item {changed.ItemId} quantity increased in inventory -> Bag {changed.ContainerId}, Slot {changed.SlotIndex} | Quantity added to sell: {changed.Difference}");
                    }
                }

                if (diff.Added.Count == 0 && diff.QuantityChanged.Count == 0) {
                    this.logger.Warning("[CancelAndSell] No inventory changes detected after canceling the sale.");
                }
            }

            this.stateMachineIndex = 0;
            this.initialSnapshot = null;
        }
    }

    public void Dispose() {
        this.framework.Update -= this.OnFrameworkUpdate;
    }
}