using Dalamud.Game.Gui.ContextMenu;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Marketeer.Core.Logging.Contracts;
using Marketeer.Core.RetainerAutomation.Contracts;
using Marketeer.UI.Localization.Contracts;
using Marketeer.UI.UiInterop.Contracts;
using System;

namespace Marketeer.UI.RetainerOverlays.Services;

public class RetainerContextMenuService : IDisposable {
    private IContextMenu contextMenu;
    private IPriceUpdateAutomationService priceUpdateService;
    private IItemCancelAndSellService itemCancelAndSellService;
    private ILocalizationService localization;
    private INativeWindowService windowService;
    private ILoggerService logger;

    public RetainerContextMenuService(
        IContextMenu contextMenu,
        IPriceUpdateAutomationService priceUpdateService,
        IItemCancelAndSellService itemCancelAndSellService,
        ILocalizationService localization,
        INativeWindowService windowService,
        ILoggerService logger) {

        this.contextMenu = contextMenu;
        this.priceUpdateService = priceUpdateService;
        this.itemCancelAndSellService = itemCancelAndSellService;
        this.localization = localization;
        this.windowService = windowService;
        this.logger = logger;

        this.contextMenu.OnMenuOpened += this.OnMenuOpened;
    }

    protected virtual unsafe uint GetTargetItemIdIfInMarket() {
        try {
            var agent = AgentInventoryContext.Instance();
            if (agent == null || agent->TargetInventorySlot == null) {
                return 0;
            }

            var inventoryManager = InventoryManager.Instance();
            if (inventoryManager == null) {
                return 0;
            }

            var container = inventoryManager->GetInventoryContainer(InventoryType.RetainerMarket);
            if (container == null || container->Size == 0) {
                return 0;
            }

            var firstSlot = container->GetInventorySlot(0);
            var lastSlot = container->GetInventorySlot(container->Size - 1);

            var targetSlot = agent->TargetInventorySlot;

            // Validate that the clicked item's memory address falls strictly within the Retainer Market container array.
            // This pointer arithmetic completely prevents the context menu from appearing on personal inventory items.
            if (targetSlot >= firstSlot && targetSlot <= lastSlot) {
                uint itemId = targetSlot->ItemId;
                return itemId > 1000000u ? itemId - 1000000u : itemId;
            }
        }
        catch (Exception ex) {
            this.logger.Error(ex, "[ContextMenu] Failed to evaluate target item memory boundary.");
        }

        return 0;
    }

    private void OnMenuOpened(IMenuOpenedArgs args) {
        try {
            // First, ensure the retainer market board is actually open
            var window = this.windowService.GetWindow("RetainerSellList");
            if (window == null || !window.IsVisible) {
                return;
            }

            uint itemId = this.GetTargetItemIdIfInMarket();

            if (itemId > 0) {
                args.AddMenuItem(new MenuItem {
                    Name = this.localization.Translate("ContextMenu_Compete"),
                    PrefixChar = 'M',
                    OnClicked = _ => this.priceUpdateService.TriggerSingleItemUpdate(itemId)
                });

                args.AddMenuItem(new MenuItem {
                    Name = this.localization.Translate("ContextMenu_CancelAndSell"),
                    PrefixChar = 'M',
                    OnClicked = _ => this.itemCancelAndSellService.TriggerCancelAndSell(itemId)
                });
            }
        }
        catch (Exception ex) {
            this.logger.Error(ex, "[ContextMenu] Failed to inject menu items.");
        }
    }

    public void Dispose() {
        this.contextMenu.OnMenuOpened -= this.OnMenuOpened;
    }
}