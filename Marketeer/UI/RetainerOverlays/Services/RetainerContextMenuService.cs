using Dalamud.Game.Gui.ContextMenu;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Marketeer.API.UiInterop.Contracts;
using Marketeer.Core.Logging.Contracts;
using Marketeer.UI.Localization.Contracts;
using System;

namespace Marketeer.UI.RetainerOverlays.Services;

public class RetainerContextMenuService : IDisposable {
    private IContextMenu contextMenu;
    private ILocalizationService localization;
    private INativeWindowService windowService;
    private ILoggerService logger;

    public RetainerContextMenuService(
        IContextMenu contextMenu,
        ILocalizationService localization,
        INativeWindowService windowService,
        ILoggerService logger) {

        this.contextMenu = contextMenu;
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
            var window = this.windowService.GetWindow("RetainerSellList");
            if (window == null || !window.IsVisible) {
                return;
            }

            uint itemId = this.GetTargetItemIdIfInMarket();

            if (itemId > 0) {
                // Point d'extension: Ajoutez ici vos éléments de menu contextuel manuels si nécessaire à l'avenir.
            }
        }
        catch (Exception ex) {
            this.logger.Error(ex, "[ContextMenu] Failed to evaluate context menu.");
        }
    }

    public void Dispose() {
        this.contextMenu.OnMenuOpened -= this.OnMenuOpened;
    }
}