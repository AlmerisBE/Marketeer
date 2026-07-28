using Dalamud.Plugin.Services;
using Marketeer.API.GameInterop.Contracts;
using Marketeer.API.Localization.Contracts;
using Marketeer.API.RetainerAutomation.Contracts;
using System;
using System.Reflection;

namespace Marketeer.Core.RetainerAutomation.Services;

public class ItemCancelAndSellService : IItemCancelAndSellService, IDisposable {
    private IRetainerUiInteractionService uiInteraction;
    private IInventoryService inventoryService;
    private ILocalizationService localization;
    private IFramework framework;

    private bool pendingMenuClick;
    private DateTime menuClickAt;

    private bool pendingInventoryOpen;
    private DateTime openInventoryAt;

    public ItemCancelAndSellService(
        IRetainerUiInteractionService uiInteraction,
        IInventoryService inventoryService,
        ILocalizationService localization,
        IFramework framework) {

        this.uiInteraction = uiInteraction;
        this.inventoryService = inventoryService;
        this.localization = localization;
        this.framework = framework;

        this.pendingMenuClick = false;
        this.pendingInventoryOpen = false;

        this.framework.Update += this.OnFrameworkUpdate;
    }

    public void TriggerCancelAndSell(object? menuTarget) {
        int uiIndex = -1;

        // Safely extract TargetIndex using Reflection to bypass strict Dalamud UI API type changes
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

        if (uiIndex != -1) {
            // Reopen the context menu seamlessly for the targeted item
            this.uiInteraction.SelectItemInSellList(uiIndex);

            this.pendingMenuClick = true;
            this.menuClickAt = DateTime.Now.AddSeconds(0.2);

            this.pendingInventoryOpen = true;
            this.openInventoryAt = DateTime.Now.AddSeconds(0.7);
        }
        else {
            // Fallback: Try an immediate click just in case the menu hasn't fully closed
            var returnText = this.localization.Translate("RetainerMenu_ReturnToInventory");
            var menuIndex = this.uiInteraction.GetContextMenuItemIndex(returnText);

            if (menuIndex == -1) {
                menuIndex = 2;
            }

            this.uiInteraction.SelectContextMenuItem(menuIndex);

            this.pendingInventoryOpen = true;
            this.openInventoryAt = DateTime.Now.AddSeconds(0.5);
        }
    }

    private void OnFrameworkUpdate(IFramework fw) {
        if (this.pendingMenuClick && DateTime.Now >= this.menuClickAt) {
            this.pendingMenuClick = false;

            var returnText = this.localization.Translate("RetainerMenu_ReturnToInventory");
            var menuIndex = this.uiInteraction.GetContextMenuItemIndex(returnText);
            if (menuIndex == -1) {
                menuIndex = 2;
            }

            this.uiInteraction.SelectContextMenuItem(menuIndex);
        }

        if (this.pendingInventoryOpen && DateTime.Now >= this.openInventoryAt) {
            this.pendingInventoryOpen = false;
            this.uiInteraction.OpenInventory();
        }
    }

    public void Dispose() {
        this.framework.Update -= this.OnFrameworkUpdate;
    }
}