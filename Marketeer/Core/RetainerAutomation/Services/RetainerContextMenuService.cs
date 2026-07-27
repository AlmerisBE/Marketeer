using Dalamud.Game.Gui.ContextMenu;
using Dalamud.Plugin.Services;
using Marketeer.API.Localization.Contracts;
using Marketeer.API.RetainerAutomation.Contracts;
using System;

namespace Marketeer.Core.RetainerAutomation.Services;

public class RetainerContextMenuService : IDisposable {
    private IContextMenu contextMenu;
    private IPriceUpdateAutomationService priceUpdateService;
    private ICancelListingsAutomationService cancelService;
    private ILocalizationService localization;

    public RetainerContextMenuService(
        IContextMenu contextMenu,
        IPriceUpdateAutomationService priceUpdateService,
        ICancelListingsAutomationService cancelService,
        ILocalizationService localization) {

        this.contextMenu = contextMenu;
        this.priceUpdateService = priceUpdateService;
        this.cancelService = cancelService;
        this.localization = localization;

        this.contextMenu.OnMenuOpened += this.OnMenuOpened;
    }

    private void OnMenuOpened(IMenuOpenedArgs args) {
        // Target the specific retainer sell list context menu
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
        this.priceUpdateService.TriggerPriceUpdate();
    }

    private void OnCancelClicked(IMenuItemClickedArgs args) {
        this.cancelService.TriggerCancellation();
    }

    public void Dispose() {
        this.contextMenu.OnMenuOpened -= this.OnMenuOpened;
    }
}