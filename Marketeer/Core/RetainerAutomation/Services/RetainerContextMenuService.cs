using Dalamud.Game.Gui.ContextMenu;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Marketeer.API.Localization.Contracts;
using Marketeer.API.Logging.Contracts;
using Marketeer.API.RetainerAutomation.Contracts;
using Marketeer.API.UiInterop.Contracts;
using System;

namespace Marketeer.Core.RetainerAutomation.Services;

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

    protected virtual unsafe uint GetTargetItemId() {
        try {
            var agent = AgentInventoryContext.Instance();
            if (agent != null && agent->TargetInventorySlot != null) {
                uint itemId = agent->TargetInventorySlot->ItemId;
                return itemId > 1000000u ? itemId - 1000000u : itemId;
            }
        }
        catch (Exception ex) {
            this.logger.Error(ex, "[ContextMenu] Failed to read AgentInventoryContext memory.");
        }

        return 0;
    }

    private void OnMenuOpened(IMenuOpenedArgs args) {
        try {
            // Strict enforcement: only inject our custom menu when interacting with the Retainer Market Board.
            // This prevents the menu from erroneously appearing on the player's personal inventory items.
            if (args.AddonName != "RetainerSellList") {
                return;
            }

            uint itemId = this.GetTargetItemId();

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