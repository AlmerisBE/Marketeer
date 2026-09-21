using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using Marketeer.API.GameInterop.Contracts;
using Marketeer.Core.Logging.Contracts;
using Marketeer.Core.RetainerAutomation.Contracts;
using Marketeer.UI.Localization.Contracts;
using System;
using System.Linq;

namespace Marketeer.Core.RetainerAutomation.Services;

public class ListingCancellationService : IListingCancellationService, IDisposable {
    private IRetainerUiInteractionService uiInteraction;
    private IInventoryService inventoryService;
    private ILocalizationService localization;
    private IFramework framework;
    private ILoggerService logger;

    private bool isActive;
    private int stateMachineIndex;
    private DateTime nextActionAt;
    private int targetUiIndex;

    public bool IsActive => this.isActive;

    public ListingCancellationService(
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

        this.framework.Update += this.OnFrameworkUpdate;
    }

    public void TriggerCancellation(uint itemId) {
        if (this.isActive) return;

        var normalizedItemId = itemId > 1000000u ? itemId - 1000000u : itemId;
        var marketSlots = this.inventoryService.GetInventorySlots(InventoryType.RetainerMarket);
        var targetSlot = marketSlots.FirstOrDefault(s => s.IsOccupied && (s.ItemId == itemId || s.ItemId == normalizedItemId || s.ItemId == normalizedItemId + 1000000u));

        if (targetSlot == null) {
            this.logger.Warning($"[ListingCancellation] Could not find item ID {itemId} in retainer market inventory.");
            return;
        }

        this.targetUiIndex = this.inventoryService.GetUiIndexForRetainerMarketItem((int)targetSlot.SlotIndex);

        if (this.targetUiIndex == -1) {
            this.logger.Warning("[ListingCancellation] Could not resolve UI Index for targeted slot.");
            return;
        }

        this.isActive = true;
        this.stateMachineIndex = 0;
        this.nextActionAt = DateTime.Now.AddSeconds(0.2);
    }

    private void OnFrameworkUpdate(IFramework fw) {
        if (!this.isActive || DateTime.Now < this.nextActionAt) return;

        switch (this.stateMachineIndex) {
            case 0:
                if (this.uiInteraction.IsAddonReady("RetainerSellList")) {
                    this.uiInteraction.SelectItemInSellList(this.targetUiIndex);
                    this.stateMachineIndex++;
                    this.nextActionAt = DateTime.Now.AddSeconds(0.2);
                }
                break;
            case 1:
                if (this.uiInteraction.IsAddonReady("ContextMenu")) {
                    var returnText = this.localization.Translate("RetainerMenu_ReturnToInventory");
                    var menuIndex = this.uiInteraction.GetContextMenuItemIndex(returnText);

                    if (menuIndex == -1) menuIndex = 1;

                    this.uiInteraction.SelectContextMenuItem(menuIndex);
                    this.logger.Info("[ListingCancellation] Successfully executed cancellation.");
                    this.isActive = false;
                }
                break;
        }
    }

    public void Dispose() {
        this.framework.Update -= this.OnFrameworkUpdate;
    }
}