using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using Marketeer.API.Configuration.Contracts;
using Marketeer.API.GameInterop.Contracts;
using Marketeer.API.Logging.Contracts;
using Marketeer.API.MarketListings.Contracts;
using Marketeer.API.MarketListings.Models;
using Marketeer.API.RetainerAutomation.Contracts;
using Marketeer.API.RetainerAutomation.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Marketeer.Core.RetainerAutomation.Services;

public enum CancelListingInternalStep {
    ProcessNextItem,
    WaitContextMenu,
    SelectStopRetaining,
    WaitYesNo,
    ConfirmYesNo,
    WaitItemRemoved
}

public class CancelListingsAutomationService : ICancelListingsAutomationService, IRetainerTask {
    private IRetainerOrchestratorService orchestrator;
    private IRetainerUiInteractionService uiInteraction;
    private IListingOptimizationService optimizationService;
    private IInventoryService inventoryService;
    private IObjectTable objectTable;
    private ILoggerService logger;
    private IConfigurationService configService;

    private Dictionary<string, Queue<SuboptimalListing>> tasksByRetainer;
    private SuboptimalListing? currentItemTask;

    private CancelListingInternalStep internalStep;
    private DateTime actionAvailableAt;
    private DateTime timeoutAt;
    private string currentRetainerName;

    public bool IsCancelling => this.orchestrator.IsActive;

    public CancelListingsAutomationService(
        IRetainerOrchestratorService orchestrator,
        IRetainerUiInteractionService uiInteraction,
        IListingOptimizationService optimizationService,
        IInventoryService inventoryService,
        IObjectTable objectTable,
        ILoggerService logger,
        IConfigurationService configService) {

        this.orchestrator = orchestrator;
        this.uiInteraction = uiInteraction;
        this.optimizationService = optimizationService;
        this.inventoryService = inventoryService;
        this.objectTable = objectTable;
        this.logger = logger;
        this.configService = configService;

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
        this.SetInternalStep(CancelListingInternalStep.ProcessNextItem, 0.5);
    }

    public bool OnTick() {
        if (DateTime.Now < this.actionAvailableAt) {
            return false;
        }

        if (DateTime.Now > this.timeoutAt && this.internalStep != CancelListingInternalStep.ProcessNextItem) {
            this.logger.Error($"Cancellation step {this.internalStep} timed out.");
            this.SetInternalStep(CancelListingInternalStep.ProcessNextItem, 0.5);
            return false;
        }

        switch (this.internalStep) {
            case CancelListingInternalStep.ProcessNextItem: return this.ProcessNextItem();
            case CancelListingInternalStep.WaitContextMenu: this.ProcessWaitContextMenu(); return false;
            case CancelListingInternalStep.SelectStopRetaining: this.ProcessSelectStopRetaining(); return false;
            case CancelListingInternalStep.WaitYesNo: this.ProcessWaitYesNo(); return false;
            case CancelListingInternalStep.ConfirmYesNo: this.ProcessConfirmYesNo(); return false;
            case CancelListingInternalStep.WaitItemRemoved: this.ProcessWaitItemRemoved(); return false;
        }

        return false;
    }

    private bool ProcessNextItem() {
        if (!this.tasksByRetainer.TryGetValue(this.currentRetainerName, out var queue) || queue.Count == 0) {
            return true;
        }

        this.currentItemTask = queue.Dequeue();

        var slots = this.inventoryService.GetInventorySlots(InventoryType.RetainerMarket);
        var targetSlot = slots.FirstOrDefault(s => s.ItemId == this.currentItemTask.ItemId && s.PricePerUnit == this.currentItemTask.CurrentPrice);

        if (targetSlot == null) {
            this.logger.Warning($"[Marketeer] Cannot find exact slot for {this.currentItemTask.ItemName}. Skipping.");
            return this.ProcessNextItem();
        }

        var uiIndex = this.inventoryService.GetUiIndexForRetainerMarketItem((int)targetSlot.SlotIndex);
        if (uiIndex == -1) {
            return this.ProcessNextItem();
        }

        this.uiInteraction.SelectItemInSellList(uiIndex);
        this.SetInternalStep(CancelListingInternalStep.WaitContextMenu, 0.2, 5.0);
        return false;
    }

    private void ProcessWaitContextMenu() {
        if (this.uiInteraction.IsAddonReady("ContextMenu")) {
            this.SetInternalStep(CancelListingInternalStep.SelectStopRetaining, 0.1);
        }
    }

    private void ProcessSelectStopRetaining() {
        if (this.currentItemTask == null) {
            return;
        }

        // Expert trick: The index shifts based on stack size
        var slots = this.inventoryService.GetInventorySlots(InventoryType.RetainerMarket);
        var targetSlot = slots.FirstOrDefault(s => s.ItemId == this.currentItemTask.ItemId && s.PricePerUnit == this.currentItemTask.CurrentPrice);

        int menuIndex = (targetSlot != null && targetSlot.Quantity > 1) ? 2 : 1;

        this.uiInteraction.SelectContextMenuItem(menuIndex);
        this.SetInternalStep(CancelListingInternalStep.WaitYesNo, 0.2, 5.0);
    }

    private void ProcessWaitYesNo() {
        if (this.uiInteraction.IsAddonReady("SelectYesNo")) {
            this.SetInternalStep(CancelListingInternalStep.ConfirmYesNo, 0.2 + this.GetRandomDelay());
        }
    }

    private void ProcessConfirmYesNo() {
        this.uiInteraction.ConfirmYesNo();
        if (this.currentItemTask != null) {
            this.logger.Info($"Cancelled suboptimal listing for '{this.currentItemTask.ItemName}'.");
        }

        this.SetInternalStep(CancelListingInternalStep.WaitItemRemoved, 0.5, 5.0);
    }

    private void ProcessWaitItemRemoved() {
        if (!this.uiInteraction.IsAddonReady("SelectYesNo")) {
            this.SetInternalStep(CancelListingInternalStep.ProcessNextItem, 0.5);
        }
    }

    private void SetInternalStep(CancelListingInternalStep step, double waitSeconds, double timeoutSeconds = 0) {
        this.internalStep = step;
        this.actionAvailableAt = DateTime.Now.AddSeconds(waitSeconds);
        if (timeoutSeconds > 0) {
            this.timeoutAt = DateTime.Now.AddSeconds(timeoutSeconds);
        }
    }

    private double GetRandomDelay() {
        var config = this.configService.GetConfig();
        if (!config.EnableAutomationDelay) {
            return 0;
        }

        var min = config.AutomationDelayMin;
        var max = config.AutomationDelayMax;
        if (min > max) {
            min = max;
        }

        return min + (new Random().NextDouble() * (max - min));
    }

    public void OnMenuClosed(string retainerName) { }
    public void OnAbort() {
        this.tasksByRetainer.Clear();
    }
}