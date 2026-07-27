using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using Marketeer.API.Configuration.Contracts;
using Marketeer.API.GameInterop.Contracts;
using Marketeer.API.Guidance.Models;
using Marketeer.API.Localization.Contracts;
using Marketeer.API.Logging.Contracts;
using Marketeer.API.MarketListings.Contracts;
using Marketeer.API.MarketListings.Models;
using Marketeer.API.RetainerAutomation.Contracts;
using Marketeer.API.RetainerAutomation.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Marketeer.Core.RetainerAutomation.Services;

public class CancelListingsAutomationService : ICancelListingsAutomationService, IRetainerTask {
    private IRetainerOrchestratorService orchestrator;
    private IRetainerUiInteractionService uiInteraction;
    private IListingOptimizationService optimizationService;
    private IInventoryService inventoryService;
    private IObjectTable objectTable;
    private ILoggerService logger;
    private IConfigurationService configService;
    private ILocalizationService localization;
    private IRetainerGuidanceService guidanceService;

    private Dictionary<string, Queue<SuboptimalListing>> tasksByRetainer;
    private SuboptimalListing? currentItemTask;

    private DateTime actionAvailableAt;
    private DateTime currentItemStartTime;
    private string currentRetainerName;

    public bool IsCancelling => this.orchestrator.IsActive;

    public CancelListingsAutomationService(
        IRetainerOrchestratorService orchestrator,
        IRetainerUiInteractionService uiInteraction,
        IListingOptimizationService optimizationService,
        IInventoryService inventoryService,
        IObjectTable objectTable,
        ILoggerService logger,
        IConfigurationService configService,
        ILocalizationService localization,
        IRetainerGuidanceService guidanceService) {

        this.orchestrator = orchestrator;
        this.uiInteraction = uiInteraction;
        this.optimizationService = optimizationService;
        this.inventoryService = inventoryService;
        this.objectTable = objectTable;
        this.logger = logger;
        this.configService = configService;
        this.localization = localization;
        this.guidanceService = guidanceService;

        this.tasksByRetainer = new Dictionary<string, Queue<SuboptimalListing>>();
        this.currentRetainerName = string.Empty;
    }

    public void TriggerCancellation() {
        if (this.IsCancelling) {
            return;
        }

        var localPlayer = this.objectTable.LocalPlayer;
        if (localPlayer == null || localPlayer.Name == null) {
            this.logger.Warning("Cannot start cancellation: No local player found.");
            return;
        }

        var playerName = localPlayer.Name.TextValue;
        var allSuboptimal = this.optimizationService.GetVendorPricedListings();
        var mySuboptimal = allSuboptimal.Where(u => u.CharacterName == playerName).ToList();

        if (!mySuboptimal.Any()) {
            this.logger.Info("No suboptimal listings detected for current character. Cancellation aborted.");
            return;
        }

        this.tasksByRetainer = mySuboptimal
            .GroupBy(u => u.RetainerName)
            .ToDictionary(g => g.Key, g => new Queue<SuboptimalListing>(g));

        var retainers = this.tasksByRetainer.Keys.ToList();
        this.orchestrator.StartOrchestration(retainers, RetainerTargetMenu.MarketListings, this);
    }

    public void AbortCancellation() {
        this.orchestrator.Abort();
    }

    public void OnMenuOpened(string retainerName) {
        this.currentRetainerName = retainerName;
        this.ProcessNextItem();
    }

    public bool OnTick() {
        if (DateTime.Now < this.actionAvailableAt) {
            return false;
        }

        if (this.currentItemTask == null) {
            return this.ProcessNextItem();
        }

        if (DateTime.Now - this.currentItemStartTime > TimeSpan.FromSeconds(15)) {
            this.logger.Error($"Timeout while cancelling {this.currentItemTask.ItemName}. Attempting recovery.");
            this.uiInteraction.CloseUnexpectedWindows();
            return this.ProcessNextItem();
        }

        var slots = this.inventoryService.GetInventorySlots(InventoryType.RetainerMarket);
        var targetSlot = slots.FirstOrDefault(s => s.ItemId == this.currentItemTask.ItemId && s.PricePerUnit == this.currentItemTask.CurrentPrice);

        // 1. Validated state: Item is no longer listed at that price
        if (targetSlot == null) {
            this.logger.Info($"Listing for '{this.currentItemTask.ItemName}' successfully cancelled. Moving to next.");
            this.guidanceService.ClearInstruction();
            return this.ProcessNextItem();
        }

        // 2. Reactive state: Confirmation dialog is open
        if (this.uiInteraction.IsAddonReady("SelectYesNo")) {
            this.uiInteraction.ConfirmYesNo();
            this.SetDelay(0.5);
            return false;
        }

        // 3. Reactive state: Context menu is open
        if (this.uiInteraction.IsAddonReady("ContextMenu")) {
            var returnText = this.localization.Translate("RetainerMenu_ReturnToInventory");
            var menuIndex = this.uiInteraction.GetContextMenuItemIndex(returnText);

            if (menuIndex != -1) {
                this.uiInteraction.SelectContextMenuItem(menuIndex);
                this.SetDelay(0.5);
                return false;
            }

            var stopText = this.localization.Translate("RetainerMenu_StopRetaining");
            menuIndex = this.uiInteraction.GetContextMenuItemIndex(stopText);

            if (menuIndex != -1) {
                this.uiInteraction.SelectContextMenuItem(menuIndex);
                this.SetDelay(0.2);
                return false;
            }

            this.uiInteraction.CloseUnexpectedWindows();
            this.SetDelay(0.5);
            return false;
        }

        // 4. Default state: Click the item in the list
        var uiIndex = this.inventoryService.GetUiIndexForRetainerMarketItem((int)targetSlot.SlotIndex);
        if (uiIndex != -1) {
            this.uiInteraction.SelectItemInSellList(uiIndex);
            this.SetDelay(0.5);
        }
        else {
            return this.ProcessNextItem();
        }

        return false;
    }

    private bool ProcessNextItem() {
        if (!this.tasksByRetainer.TryGetValue(this.currentRetainerName, out var queue) || queue.Count == 0) {
            return true;
        }

        this.currentItemTask = queue.Dequeue();
        this.currentItemStartTime = DateTime.Now;
        this.SetDelay(0.5);

        this.guidanceService.SetInstruction(new GuidanceInstruction {
            ActionType = GuidanceActionType.CancelListing,
            ItemName = this.currentItemTask.ItemName
        });

        return false;
    }

    private void SetDelay(double waitSeconds) {
        var config = this.configService.GetConfig();
        double randomDelay = 0;

        if (config.EnableAutomationDelay) {
            var min = config.AutomationDelayMin;
            var max = config.AutomationDelayMax;
            if (min > max) {
                min = max;
            }

            randomDelay = min + (new Random().NextDouble() * (max - min));
        }

        this.actionAvailableAt = DateTime.Now.AddSeconds(waitSeconds + randomDelay);
    }

    public void OnMenuClosed(string retainerName) {
        this.guidanceService.ClearInstruction();
    }

    public void OnAbort() {
        this.tasksByRetainer.Clear();
        this.guidanceService.ClearInstruction();
    }
}