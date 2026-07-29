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

    private void OnMenuOpened(IMenuOpenedArgs args) {
        if (args.AddonName == "RetainerSellList") {
            args.AddMenuItem(new MenuItem {
                Name = this.localization.Translate("ContextMenu_Compete"),
                OnClicked = this.OnCompeteClicked
            });

            args.AddMenuItem(new MenuItem {
                Name = this.localization.Translate("ContextMenu_CancelAndSell"),
                OnClicked = this.OnCancelClicked
            });
        }
    }

    private void OnCompeteClicked(IMenuItemClickedArgs args) {
        this.priceUpdateService.TriggerSingleItemUpdate(args.Target);
    }

    private void OnCancelClicked(IMenuItemClickedArgs args) {
        this.itemCancelAndSellService.TriggerCancelAndSell(args.Target);
    }

    public void Dispose() {
        this.contextMenu.OnMenuOpened -= this.OnMenuOpened;
    }
}