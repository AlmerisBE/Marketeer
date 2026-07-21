using Dalamud.Plugin.Services;
using Marketeer.Features.Inventory.Contracts;
using Marketeer.Features.Logging.Contracts;
using Marketeer.Features.RetainerAutomation.Contracts;
using Marketeer.Features.RetainerOrchestration.Contracts;
using Marketeer.Features.RetainerOrchestration.Models;
using Marketeer.Features.UndercutTracking.Contracts;
using Marketeer.Features.UndercutTracking.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Marketeer.Features.RetainerAutomation.Services;

public enum PriceUpdateInternalStep {
    ProcessNextItem,
    WaitContextMenu,
    SelectAdjustPrice,
    WaitItemMenu,
    ChangePrice,
    WaitPriceApplied
}

public class PriceUpdateAutomationService : IPriceUpdateAutomationService, IRetainerTask {
    private IRetainerOrchestratorService orchestrator;
    private IUiInteractionService uiInteraction;
    private ICompetitionStateService competitionState;
    private IInventoryService inventoryService;
    private IObjectTable objectTable;
    private ILoggerService logger;

    private Dictionary<string, Queue<UndercutItem>> tasksByRetainer;
    private UndercutItem? currentItemTask;

    private PriceUpdateInternalStep internalStep;
    private int waitTicks;
    private int internalTimeout;
    private string currentRetainerName;

    public bool IsUpdating => this.orchestrator.IsActive;

    public PriceUpdateAutomationService(
        IRetainerOrchestratorService orchestrator,
        IUiInteractionService uiInteraction,
        ICompetitionStateService competitionState,
        IInventoryService inventoryService,
        IObjectTable objectTable,
        ILoggerService logger) {

        this.orchestrator = orchestrator;
        this.uiInteraction = uiInteraction;
        this.competitionState = competitionState;
        this.inventoryService = inventoryService;
        this.objectTable = objectTable;
        this.logger = logger;

        this.tasksByRetainer = new Dictionary<string, Queue<UndercutItem>>();
        this.currentRetainerName = string.Empty;
    }

    public void TriggerPriceUpdate() {
        if (this.IsUpdating) {
            return;
        }

        var localPlayer = this.objectTable.LocalPlayer;
        if (localPlayer == null || localPlayer.Name == null) {
            this.logger.Warning("Cannot start price update: No local player found.");
            return;
        }

        var playerName = localPlayer.Name.TextValue;
        var allUndercuts = this.competitionState.GetUndercutItems();
        var myUndercuts = allUndercuts.Where(u => u.CharacterName == playerName).ToList();

        if (!myUndercuts.Any()) {
            this.logger.Info("No undercuts detected for current character. Price update aborted.");
            return;
        }

        this.tasksByRetainer = myUndercuts
            .GroupBy(u => u.RetainerName)
            .ToDictionary(g => g.Key, g => new Queue<UndercutItem>(g));

        var retainers = this.tasksByRetainer.Keys.ToList();
        this.orchestrator.StartOrchestration(retainers, RetainerTargetMenu.MarketListings, this);
    }

    public void AbortUpdate() {
        this.orchestrator.Abort();
    }

    public void OnMenuOpened(string retainerName) {
        this.currentRetainerName = retainerName;
        this.SetInternalStep(PriceUpdateInternalStep.ProcessNextItem, 15);
    }

    public bool OnTick() {
        if (this.waitTicks > 0) {
            this.waitTicks--;
            return false;
        }

        if (this.internalTimeout > 0) {
            this.internalTimeout--;
        }

        switch (this.internalStep) {
            case PriceUpdateInternalStep.ProcessNextItem:
                return this.ProcessNextItem();
            case PriceUpdateInternalStep.WaitContextMenu:
                this.ProcessWaitContextMenu();
                return false;
            case PriceUpdateInternalStep.SelectAdjustPrice:
                this.ProcessSelectAdjustPrice();
                return false;
            case PriceUpdateInternalStep.WaitItemMenu:
                this.ProcessWaitItemMenu();
                return false;
            case PriceUpdateInternalStep.ChangePrice:
                this.ProcessChangePrice();
                return false;
            case PriceUpdateInternalStep.WaitPriceApplied:
                this.ProcessWaitPriceApplied();
                return false;
        }

        return false;
    }

    private bool ProcessNextItem() {
        if (!this.tasksByRetainer.TryGetValue(this.currentRetainerName, out var queue) || queue.Count == 0) {
            return true;
        }

        this.currentItemTask = queue.Dequeue();

        var uiIndex = this.inventoryService.GetUiIndexForRetainerMarketItem(this.currentItemTask.SlotIndex);

        if (uiIndex == -1) {
            this.logger.Warning($"[Marketeer] Cannot find UI index for slot {this.currentItemTask.SlotIndex} (Item: {this.currentItemTask.ItemName}). Skipping.");
            return this.ProcessNextItem();
        }

        this.uiInteraction.SelectItemInSellList(uiIndex);
        this.SetInternalStep(PriceUpdateInternalStep.WaitContextMenu, 5, 120);
        return false;
    }

    private void ProcessWaitContextMenu() {
        if (this.uiInteraction.IsAddonReady("ContextMenu")) {
            this.SetInternalStep(PriceUpdateInternalStep.SelectAdjustPrice, 2);
        }
        else if (this.internalTimeout <= 0) {
            this.logger.Warning($"[Marketeer] Failed to open ContextMenu for item '{this.currentItemTask?.ItemName}'. Skipping to next item.");
            this.SetInternalStep(PriceUpdateInternalStep.ProcessNextItem, 5);
        }
    }

    private void ProcessSelectAdjustPrice() {
        this.uiInteraction.SelectContextMenuItem(0);
        this.SetInternalStep(PriceUpdateInternalStep.WaitItemMenu, 5, 120);
    }

    private void ProcessWaitItemMenu() {
        if (this.uiInteraction.IsAddonReady("RetainerSell")) {
            this.SetInternalStep(PriceUpdateInternalStep.ChangePrice, 10);
        }
        else if (this.internalTimeout <= 0) {
            this.logger.Warning($"[Marketeer] Failed to open RetainerSell window for item '{this.currentItemTask?.ItemName}'. Skipping to next item.");
            this.SetInternalStep(PriceUpdateInternalStep.ProcessNextItem, 5);
        }
    }

    private void ProcessChangePrice() {
        if (this.currentItemTask != null) {
            uint newPrice = Math.Max(1u, this.currentItemTask.ServerCheapestPrice - 1);
            this.uiInteraction.ConfirmPriceUpdate(newPrice);
            this.logger.Info($"Updated item '{this.currentItemTask.ItemName}' to {newPrice} Gil.");

            this.SetInternalStep(PriceUpdateInternalStep.WaitPriceApplied, 15, 120);
        }
    }

    private void ProcessWaitPriceApplied() {
        if (!this.uiInteraction.IsAddonReady("RetainerSell")) {
            this.SetInternalStep(PriceUpdateInternalStep.ProcessNextItem, 15);
        }
        else if (this.internalTimeout <= 0) {
            this.logger.Warning($"[Marketeer] RetainerSell window got stuck after price update for '{this.currentItemTask?.ItemName}'. Forcing closure.");
            this.uiInteraction.CloseRetainerMarket();
            this.SetInternalStep(PriceUpdateInternalStep.ProcessNextItem, 15);
        }
    }

    private void SetInternalStep(PriceUpdateInternalStep step, int wait, int timeout = 0) {
        this.internalStep = step;
        this.waitTicks = wait;
        this.internalTimeout = timeout;
    }

    public void OnMenuClosed(string retainerName) {
        // Aucune action nécessaire
    }

    public void OnAbort() {
        this.tasksByRetainer.Clear();
    }
}