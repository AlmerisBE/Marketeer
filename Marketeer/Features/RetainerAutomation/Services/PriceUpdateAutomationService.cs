using Dalamud.Plugin.Services;
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
    WaitItemMenu,
    ChangePrice,
    WaitPriceApplied
}

public class PriceUpdateAutomationService : IPriceUpdateAutomationService, IRetainerTask {
    private IRetainerOrchestratorService orchestrator;
    private IUiInteractionService uiInteraction;
    private ICompetitionStateService competitionState;
    private IObjectTable objectTable;
    private ILoggerService logger;

    private Dictionary<string, Queue<UndercutItem>> tasksByRetainer;
    private UndercutItem? currentItemTask;

    private PriceUpdateInternalStep internalStep;
    private int waitTicks;
    private string currentRetainerName;

    public bool IsUpdating => this.orchestrator.IsActive;

    public PriceUpdateAutomationService(
        IRetainerOrchestratorService orchestrator,
        IUiInteractionService uiInteraction,
        ICompetitionStateService competitionState,
        IObjectTable objectTable,
        ILoggerService logger) {

        this.orchestrator = orchestrator;
        this.uiInteraction = uiInteraction;
        this.competitionState = competitionState;
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
        this.internalStep = PriceUpdateInternalStep.ProcessNextItem;
        this.waitTicks = 15;
    }

    public bool OnTick() {
        if (this.waitTicks > 0) {
            this.waitTicks--;
            return false;
        }

        switch (this.internalStep) {
            case PriceUpdateInternalStep.ProcessNextItem:
                return this.ProcessNextItem();
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
            return true; // Finished all items for this retainer
        }

        this.currentItemTask = queue.Dequeue();
        this.uiInteraction.SelectItemInSellList(this.currentItemTask.SlotIndex);

        this.internalStep = PriceUpdateInternalStep.WaitItemMenu;
        this.waitTicks = 15;
        return false;
    }

    private void ProcessWaitItemMenu() {
        if (this.uiInteraction.IsAddonReady("RetainerSell")) {
            this.internalStep = PriceUpdateInternalStep.ChangePrice;
            this.waitTicks = 10;
        }
    }

    private void ProcessChangePrice() {
        if (this.currentItemTask != null) {
            uint newPrice = Math.Max(1u, this.currentItemTask.ServerCheapestPrice - 1);
            this.uiInteraction.ConfirmPriceUpdate(newPrice);
            this.logger.Info($"Updated item '{this.currentItemTask.ItemName}' at slot {this.currentItemTask.SlotIndex} to {newPrice} Gil.");

            this.internalStep = PriceUpdateInternalStep.WaitPriceApplied;
            this.waitTicks = 30;
        }
    }

    private void ProcessWaitPriceApplied() {
        if (!this.uiInteraction.IsAddonReady("RetainerSell")) {
            this.internalStep = PriceUpdateInternalStep.ProcessNextItem;
            this.waitTicks = 15;
        }
    }

    public void OnMenuClosed(string retainerName) {
        // Aucune action nécessaire
    }

    public void OnAbort() {
        this.tasksByRetainer.Clear();
    }
}