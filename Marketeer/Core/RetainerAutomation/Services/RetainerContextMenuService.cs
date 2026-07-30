using Dalamud.Game.Gui.ContextMenu;
using Dalamud.Plugin.Services;
using Marketeer.API.Localization.Contracts;
using Marketeer.API.RetainerAutomation.Contracts;
using System;

namespace Marketeer.Core.RetainerAutomation.Services;

public class RetainerContextMenuService : IDisposable {
    private IContextMenu contextMenu;
    private IPriceUpdateAutomationService priceUpdateService;
    private IItemCancelAndSellService itemCancelAndSellService;
    private ILocalizationService localization;

    public RetainerContextMenuService(
        IContextMenu contextMenu,
        IPriceUpdateAutomationService priceUpdateService,
        IItemCancelAndSellService itemCancelAndSellService,
        ILocalizationService localization) {

        this.contextMenu = contextMenu;
        this.priceUpdateService = priceUpdateService;
        this.itemCancelAndSellService = itemCancelAndSellService;
        this.localization = localization;

        this.contextMenu.OnMenuOpened += this.OnMenuOpened;
    }

    protected virtual uint GetTargetItemId(MenuTarget target) {
        if (target is MenuTargetInventory inventoryTarget) {
            return inventoryTarget.TargetItem?.ItemId ?? 0;
        }
        return 0;
    }

    private void OnMenuOpened(IMenuOpenedArgs args) {
        if (args.AddonName == "RetainerSellList") {
            var itemId = this.GetTargetItemId(args.Target);
            if (itemId == 0) {
                return;
            }

            args.AddMenuItem(new MenuItem {
                Name = this.localization.Translate("ContextMenu_Compete"),
                OnClicked = _ => this.priceUpdateService.TriggerSingleItemUpdate(itemId)
            });

            args.AddMenuItem(new MenuItem {
                Name = this.localization.Translate("ContextMenu_CancelAndSell"),
                OnClicked = _ => this.itemCancelAndSellService.TriggerCancelAndSell(itemId)
            });
        }
    }

    public void Dispose() {
        this.contextMenu.OnMenuOpened -= this.OnMenuOpened;
    }
}