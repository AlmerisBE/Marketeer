using Dalamud.Plugin.Services;
using Marketeer.Features.Logging.Contracts;
using Marketeer.Features.RetainerAutomation.Contracts;
using Marketeer.Features.Retainers.Contracts;
using Marketeer.Features.UndercutTracking.Contracts;
using Marketeer.Features.UndercutTracking.Models;
using Marketeer.Features.WindowAbstraction.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Marketeer.Features.RetainerAutomation.Services;

public enum PriceUpdateStep {
    Idle,
    SelectRetainer,
    OpenSellList,
    WaitSellList,
    ProcessNextItem,
    WaitItemMenu,
    ChangePrice,
    WaitPriceApplied,
    CloseSellList,
    CloseSelectString,
    CloseRetainerList
}

public class PriceUpdateAutomationService : IPriceUpdateAutomationService, IDisposable {
    private IFramework framework;
    private IRetainerService retainerService;
    private IUiInteractionService uiInteraction;
    private ICompetitionStateService competitionState;
    private IObjectTable objectTable;
    private INativeWindowService windowService;
    private ILoggerService logger;

    private Dictionary<string, Queue<UndercutItem>> tasksByRetainer;
    private Queue<string> retainerQueue;
    private string currentRetainerName;
    private UndercutItem? currentItemTask;

    private PriceUpdateStep currentStep;
    private int cooldownTicks;
    private int timeoutTicks;

    public bool IsUpdating { get; private set; }

    public PriceUpdateAutomationService(
        IFramework framework,
        IRetainerService retainerService,
        IUiInteractionService uiInteraction,
        ICompetitionStateService competitionState,
        IObjectTable objectTable,
        INativeWindowService windowService,
        ILoggerService logger) {

        this.framework = framework;
        this.retainerService = retainerService;
        this.uiInteraction = uiInteraction;
        this.competitionState = competitionState;
        this.objectTable = objectTable;
        this.windowService = windowService;
        this.logger = logger;

        this.tasksByRetainer = new Dictionary<string, Queue<UndercutItem>>();
        this.retainerQueue = new Queue<string>();
        this.currentRetainerName = string.Empty;
        this.currentStep = PriceUpdateStep.Idle;

        this.framework.Update += this.OnFrameworkUpdate;
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

        // Group the undercuts by retainer to process them sequentially
        this.tasksByRetainer = myUndercuts
            .GroupBy(u => u.RetainerName)
            .ToDictionary(g => g.Key, g => new Queue<UndercutItem>(g));

        this.retainerQueue = new Queue<string>(this.tasksByRetainer.Keys);

        this.IsUpdating = true;
        this.logger.Info($"Starting automated price update for {this.retainerQueue.Count} retainers.");

        this.AdvanceToNextRetainerOrFinish();
    }

    public void AbortUpdate() {
        this.IsUpdating = false;
        this.currentStep = PriceUpdateStep.Idle;
        this.logger.Info("Automated price update sequence aborted/concluded.");
    }

    private void OnFrameworkUpdate(IFramework frameworkInstance) {
        if (!this.IsUpdating) {
            return;
        }

        if (this.cooldownTicks > 0) {
            this.cooldownTicks--;
            return;
        }

        if (this.timeoutTicks <= 0) {
            this.logger.Error($"Price update automation step {this.currentStep} timed out. Aborting.");
            this.AbortUpdate();
            return;
        }

        this.timeoutTicks--;

        switch (this.currentStep) {
            case PriceUpdateStep.SelectRetainer:
                this.ProcessSelectRetainer();
                break;
            case PriceUpdateStep.OpenSellList:
                this.ProcessOpenSellList();
                break;
            case PriceUpdateStep.WaitSellList:
                this.ProcessWaitSellList();
                break;
            case PriceUpdateStep.ProcessNextItem:
                this.ProcessNextItem();
                break;
            case PriceUpdateStep.WaitItemMenu:
                this.ProcessWaitItemMenu();
                break;
            case PriceUpdateStep.ChangePrice:
                this.ProcessChangePrice();
                break;
            case PriceUpdateStep.WaitPriceApplied:
                this.ProcessWaitPriceApplied();
                break;
            case PriceUpdateStep.CloseSellList:
                this.ProcessCloseSellList();
                break;
            case PriceUpdateStep.CloseSelectString:
                this.ProcessCloseSelectString();
                break;
            case PriceUpdateStep.CloseRetainerList:
                this.ProcessCloseRetainerList();
                break;
        }
    }

    private void AdvanceToNextRetainerOrFinish() {
        if (this.retainerQueue.Count > 0) {
            this.currentRetainerName = this.retainerQueue.Dequeue();
            this.SetState(PriceUpdateStep.SelectRetainer, 15);
        }
        else {
            this.SetState(PriceUpdateStep.CloseRetainerList, 15);
        }
    }

    private void ProcessSelectRetainer() {
        if (this.retainerService.IsRetainerAvailable(this.currentRetainerName)) {
            if (this.retainerService.SelectRetainer(this.currentRetainerName)) {
                this.SetState(PriceUpdateStep.OpenSellList, 15);
            }
            else {
                this.AbortUpdate();
            }
        }
    }

    private void ProcessOpenSellList() {
        if (this.retainerService.IsMenuReadyForRetainer(this.currentRetainerName)) {
            this.uiInteraction.OpenRetainerMarket();
            this.SetState(PriceUpdateStep.WaitSellList, 30);
        }
    }

    private void ProcessWaitSellList() {
        if (this.uiInteraction.IsAddonReady("RetainerSellList")) {
            this.SetState(PriceUpdateStep.ProcessNextItem, 15);
        }
    }

    private void ProcessNextItem() {
        var queue = this.tasksByRetainer[this.currentRetainerName];

        if (queue.Count > 0) {
            this.currentItemTask = queue.Dequeue();
            this.uiInteraction.SelectItemInSellList(this.currentItemTask.SlotIndex);
            this.SetState(PriceUpdateStep.WaitItemMenu, 15);
        }
        else {
            this.SetState(PriceUpdateStep.CloseSellList, 15);
        }
    }

    private void ProcessWaitItemMenu() {
        if (this.uiInteraction.IsAddonReady("RetainerSell")) {
            this.SetState(PriceUpdateStep.ChangePrice, 10);
        }
    }

    private void ProcessChangePrice() {
        if (this.currentItemTask != null) {
            // Apply logic: Lowest price minus 1 Gil, minimum 1 Gil.
            uint newPrice = Math.Max(1u, this.currentItemTask.ServerCheapestPrice - 1);

            this.uiInteraction.ConfirmPriceUpdate(newPrice);
            this.logger.Info($"Updated item '{this.currentItemTask.ItemName}' at slot {this.currentItemTask.SlotIndex} to {newPrice} Gil.");

            this.SetState(PriceUpdateStep.WaitPriceApplied, 30);
        }
    }

    private void ProcessWaitPriceApplied() {
        if (!this.uiInteraction.IsAddonReady("RetainerSell")) {
            this.SetState(PriceUpdateStep.ProcessNextItem, 15);
        }
    }

    private void ProcessCloseSellList() {
        if (this.retainerService.CloseMarketListings()) {
            this.SetState(PriceUpdateStep.CloseSelectString, 60);
        }
    }

    private void ProcessCloseSelectString() {
        if (this.retainerService.IsMenuReadyForRetainer(this.currentRetainerName)) {
            if (this.retainerService.CloseRetainerMenu()) {
                this.AdvanceToNextRetainerOrFinish();
            }
        }
    }

    private void ProcessCloseRetainerList() {
        var window = this.windowService.GetWindow("RetainerList");
        if (window != null && window.IsVisible) {
            window.SendCallback(-1);
            this.AbortUpdate();
        }
    }

    private void SetState(PriceUpdateStep nextStep, int initialCooldown) {
        this.currentStep = nextStep;
        this.cooldownTicks = initialCooldown;
        this.timeoutTicks = 600;
    }

    public void Dispose() {
        this.framework.Update -= this.OnFrameworkUpdate;
    }
}