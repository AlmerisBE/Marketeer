using Dalamud.Game.Gui.ContextMenu;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Marketeer.API.GameInterop.Contracts;
using Marketeer.API.Localization.Contracts;
using Marketeer.API.Logging.Contracts;
using Marketeer.API.RetainerAutomation.Contracts;
using Marketeer.API.UiInterop.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Marketeer.Core.RetainerAutomation.Services;

public class RetainerContextMenuService : IDisposable {
    private IContextMenu contextMenu;
    private IPriceUpdateAutomationService priceUpdateService;
    private IItemCancelAndSellService itemCancelAndSellService;
    private ILocalizationService localization;
    private INativeWindowService windowService;
    private IInventoryService inventoryService;
    private ILoggerService logger;

    public RetainerContextMenuService(
        IContextMenu contextMenu,
        IPriceUpdateAutomationService priceUpdateService,
        IItemCancelAndSellService itemCancelAndSellService,
        ILocalizationService localization,
        INativeWindowService windowService,
        IInventoryService inventoryService,
        ILoggerService logger) {

        this.contextMenu = contextMenu;
        this.priceUpdateService = priceUpdateService;
        this.itemCancelAndSellService = itemCancelAndSellService;
        this.localization = localization;
        this.windowService = windowService;
        this.inventoryService = inventoryService;
        this.logger = logger;

        this.contextMenu.OnMenuOpened += this.OnMenuOpened;
    }

    protected virtual unsafe int GetTargetIndex(IMenuOpenedArgs args) {
        try {
            var agentModule = AgentModule.Instance();
            if (agentModule != null) {
                // Focus the memory dump on AgentInventoryContext, which handles item-specific context menus
                var inventoryContextAgent = agentModule->GetAgentByInternalId(AgentId.InventoryContext);
                if (inventoryContextAgent != null) {
                    int* agentPtr = (int*)inventoryContextAgent;
                    var values = new List<string>();

                    for (int i = 0; i < 128; i++) {
                        int val = agentPtr[i];
                        if (val >= 0 && val < 50) {
                            values.Add($"[+{i * 4}]={val}");
                        }
                    }

                    this.logger.Warning($"[ContextMenu] AgentInventoryContext memory dump: {string.Join(", ", values)}");
                }
            }
        }
        catch (Exception ex) {
            this.logger.Error(ex, "[ContextMenu] Failed to read AgentInventoryContext memory.");
        }

        return -1;
    }

    private void OnMenuOpened(IMenuOpenedArgs args) {
        try {
            bool isRetainerMenu = args.AddonName == "RetainerSellList";

            if (!isRetainerMenu) {
                var window = this.windowService.GetWindow("RetainerSellList");
                if (window != null && window.IsVisible) {
                    isRetainerMenu = true;
                }
            }

            if (isRetainerMenu) {
                int targetIndex = this.GetTargetIndex(args);

                args.AddMenuItem(new MenuItem {
                    Name = this.localization.Translate("ContextMenu_Compete"),
                    PrefixChar = 'M',
                    OnClicked = _ => this.HandleCompeteClicked(targetIndex)
                });

                args.AddMenuItem(new MenuItem {
                    Name = this.localization.Translate("ContextMenu_CancelAndSell"),
                    PrefixChar = 'M',
                    OnClicked = _ => this.HandleCancelAndSellClicked(targetIndex)
                });
            }
        }
        catch (Exception ex) {
            this.logger.Error(ex, "[ContextMenu] Failed to inject menu items.");
        }
    }

    private void HandleCompeteClicked(int targetIndex) {
        uint itemId = this.ResolveItemIdFromUiIndex(targetIndex);
        if (itemId > 0) {
            this.priceUpdateService.TriggerSingleItemUpdate(itemId);
        }
    }

    private void HandleCancelAndSellClicked(int targetIndex) {
        uint itemId = this.ResolveItemIdFromUiIndex(targetIndex);
        if (itemId > 0) {
            this.itemCancelAndSellService.TriggerCancelAndSell(itemId);
        }
    }

    private uint ResolveItemIdFromUiIndex(int targetIndex) {
        if (targetIndex == -1) {
            this.logger.Warning("[ContextMenu] Cannot interact: UI index could not be determined.");
            return 0;
        }

        var occupiedSlots = this.inventoryService.GetInventorySlots(InventoryType.RetainerMarket)
            .Where(s => s.IsOccupied)
            .OrderBy(s => s.SlotIndex)
            .ToList();

        if (targetIndex >= 0 && targetIndex < occupiedSlots.Count) {
            return occupiedSlots[targetIndex].ItemId;
        }

        this.logger.Warning($"[ContextMenu] TargetIndex {targetIndex} is out of bounds for {occupiedSlots.Count} occupied slots.");
        return 0;
    }

    public void Dispose() {
        this.contextMenu.OnMenuOpened -= this.OnMenuOpened;
    }
}