using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using Marketeer.API.GameInterop.Contracts;
using Marketeer.Core.Configuration.Contracts;
using Marketeer.Core.Configuration.Models;
using Marketeer.Core.Logging.Contracts;
using Marketeer.Core.RetainerAutomation.Contracts;
using Marketeer.Core.SalesHistory.Contracts;
using Marketeer.UI.Localization.Contracts;
using System;
using System.Linq;

namespace Marketeer.Core.RetainerAutomation.Services;

public class ListingCancellationService : IListingCancellationService, IDisposable {
    private IRetainerUiInteractionService uiInteraction;
    private IInventoryService inventoryService;
    private IConfigurationService configService;
    private IItemResolverService itemResolver;
    private ILocalizationService localization;
    private IFramework framework;
    private ILoggerService logger;
    private INotificationService notificationService;
    private IAutomationDelayProvider delayProvider;

    private bool isActive;
    private int stateMachineIndex;
    private DateTime nextActionAt;
    private int targetUiIndex;
    private uint currentItemId;

    public bool IsActive => this.isActive;

    public ListingCancellationService(
        IRetainerUiInteractionService uiInteraction,
        IInventoryService inventoryService,
        IConfigurationService configService,
        IItemResolverService itemResolver,
        ILocalizationService localization,
        IFramework framework,
        ILoggerService logger,
        INotificationService notificationService,
        IAutomationDelayProvider delayProvider) {

        this.uiInteraction = uiInteraction;
        this.inventoryService = inventoryService;
        this.configService = configService;
        this.itemResolver = itemResolver;
        this.localization = localization;
        this.framework = framework;
        this.logger = logger;
        this.notificationService = notificationService;
        this.delayProvider = delayProvider;

        this.framework.Update += this.OnFrameworkUpdate;
    }

    public void TriggerCancellation(uint itemId) {
        if (this.isActive) return;

        var normalizedItemId = itemId > 1000000u ? itemId - 1000000u : itemId;
        var marketSlots = this.inventoryService.GetInventorySlots(InventoryType.RetainerMarket);
        var targetSlot = marketSlots.FirstOrDefault(s => s.IsOccupied && (s.ItemId == itemId || s.ItemId == normalizedItemId));

        if (targetSlot == null) {
            this.logger.Warning($"[ListingCancellation] Could not find item ID {itemId} in retainer market inventory.");
            return;
        }

        this.targetUiIndex = this.inventoryService.GetUiIndexForRetainerMarketItem((int)targetSlot.SlotIndex);

        if (this.targetUiIndex == -1) {
            this.logger.Warning("[ListingCancellation] Could not resolve UI Index for targeted slot.");
            return;
        }

        this.currentItemId = itemId;
        this.isActive = true;
        this.stateMachineIndex = 0;
        this.nextActionAt = DateTime.Now.Add(this.delayProvider.GetDelay(100));
    }

    private void OnFrameworkUpdate(IFramework fw) {
        if (!this.isActive || DateTime.Now < this.nextActionAt) return;

        switch (this.stateMachineIndex) {
            case 0:
                if (this.uiInteraction.IsAddonReady("ContextMenu")) {
                    this.stateMachineIndex = 1;
                    this.nextActionAt = DateTime.Now.Add(this.delayProvider.GetDelay(100));
                }
                else {
                    this.uiInteraction.CloseUnexpectedWindows(); // Ensure RetainerSell is closed
                    this.uiInteraction.OpenContextMenuForSellList(this.targetUiIndex);
                    this.stateMachineIndex = 1;
                    this.nextActionAt = DateTime.Now.Add(this.delayProvider.GetDelay(300)); // Wait for menu to open
                }
                break;
            case 1:
                var config = this.configService.GetConfig();
                string translationKey = config.CancelInventoryPriority == InventoryPriority.PlayerFirst
                    ? "RetainerMenu_ReturnToInventory"
                    : "RetainerMenu_ReturnToRetainer";

                var returnText = this.localization.Translate(translationKey);
                var menuIndex = this.uiInteraction.GetContextMenuItemIndex(returnText);

                if (menuIndex == -1) {
                    menuIndex = config.CancelInventoryPriority == InventoryPriority.PlayerFirst ? 1 : 2;
                    this.logger.Warning($"[ListingCancellation] Context menu option not found. Using native fallback index {menuIndex}.");
                }

                this.uiInteraction.SelectContextMenuItem(menuIndex);

                string itemName = this.itemResolver.ResolveItemName(this.currentItemId) ?? $"Item #{this.currentItemId}";
                var format = this.localization.Translate("Notification_Cancellation_Success") ?? "{0} cancelled to prevent loss.";
                this.notificationService.ShowWarning("Marketeer", string.Format(format, itemName));

                this.logger.Info("[ListingCancellation] Cancellation executed successfully.");
                this.isActive = false;
                break;
        }
    }

    public void Dispose() {
        this.framework.Update -= this.OnFrameworkUpdate;
    }
}