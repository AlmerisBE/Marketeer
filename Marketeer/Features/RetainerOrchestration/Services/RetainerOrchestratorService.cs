using Dalamud.Plugin.Services;
using Marketeer.Features.Localization.Contracts;
using Marketeer.Features.Logging.Contracts;
using Marketeer.Features.RetainerAutomation.Contracts;
using Marketeer.Features.RetainerOrchestration.Contracts;
using Marketeer.Features.RetainerOrchestration.Models;
using Marketeer.Features.WindowAbstraction.Contracts;
using System;
using System.Collections.Generic;

namespace Marketeer.Features.RetainerOrchestration.Services;

public enum OrchestrationStep {
    Idle,
    SelectRetainer,
    OpenMenu,
    WaitMenu,
    ExecutingTask,
    CloseMenu,
    WaitMenuClosed,
    CloseSelectString,
    CloseRetainerList
}

public class RetainerOrchestratorService : IRetainerOrchestratorService, IDisposable {
    private IFramework framework;
    private IRetainerUiInteractionService uiInteractionService;
    private INativeWindowService windowService;
    private ILocalizationService localizationService;
    private ILoggerService logger;

    private Queue<string> retainerQueue;
    private string currentRetainerName;
    private RetainerTargetMenu currentTargetMenu;
    private IRetainerTask? currentTask;

    private OrchestrationStep currentStep;
    private int cooldownTicks;
    private int timeoutTicks;

    public bool IsActive { get; private set; }

    public RetainerOrchestratorService(
        IFramework framework,
        IRetainerUiInteractionService uiInteractionService,
        INativeWindowService windowService,
        ILocalizationService localizationService,
        ILoggerService logger) {

        this.framework = framework;
        this.uiInteractionService = uiInteractionService;
        this.windowService = windowService;
        this.localizationService = localizationService;
        this.logger = logger;

        this.retainerQueue = new Queue<string>();
        this.currentRetainerName = string.Empty;
        this.currentStep = OrchestrationStep.Idle;

        this.framework.Update += this.OnFrameworkUpdate;
    }

    public void StartOrchestration(IEnumerable<string> retainerNames, RetainerTargetMenu targetMenu, IRetainerTask task) {
        if (this.IsActive) {
            this.logger.Warning("Cannot start orchestration: another orchestration is already active.");
            return;
        }

        this.retainerQueue = new Queue<string>(retainerNames);
        if (this.retainerQueue.Count == 0) {
            return;
        }

        this.currentTargetMenu = targetMenu;
        this.currentTask = task;
        this.IsActive = true;
        this.logger.Info($"Starting Retainer Orchestration for {this.retainerQueue.Count} retainers. Target Menu: {targetMenu}.");

        this.AdvanceToNextRetainerOrFinish();
    }

    public void Abort() {
        this.IsActive = false;
        this.currentStep = OrchestrationStep.Idle;
        this.currentTask?.OnAbort();
        this.currentTask = null;
        this.logger.Info("Retainer orchestration sequence aborted/concluded.");
    }

    private void OnFrameworkUpdate(IFramework frameworkInstance) {
        if (!this.IsActive) {
            return;
        }

        if (this.cooldownTicks > 0) {
            this.cooldownTicks--;
            return;
        }

        if (this.timeoutTicks <= 0) {
            this.logger.Error($"Orchestration step {this.currentStep} timed out. Aborting.");
            this.Abort();
            return;
        }

        this.timeoutTicks--;

        switch (this.currentStep) {
            case OrchestrationStep.SelectRetainer:
                this.ProcessSelectRetainer();
                break;
            case OrchestrationStep.OpenMenu:
                this.ProcessOpenMenu();
                break;
            case OrchestrationStep.WaitMenu:
                this.ProcessWaitMenu();
                break;
            case OrchestrationStep.ExecutingTask:
                this.ProcessExecutingTask();
                break;
            case OrchestrationStep.CloseMenu:
                this.ProcessCloseMenu();
                break;
            case OrchestrationStep.WaitMenuClosed:
                this.ProcessWaitMenuClosed();
                break;
            case OrchestrationStep.CloseSelectString:
                this.ProcessCloseSelectString();
                break;
            case OrchestrationStep.CloseRetainerList:
                this.ProcessCloseRetainerList();
                break;
        }
    }

    private void AdvanceToNextRetainerOrFinish() {
        if (this.retainerQueue.Count > 0) {
            this.currentRetainerName = this.retainerQueue.Dequeue();
            this.SetState(OrchestrationStep.SelectRetainer, 15);
        }
        else {
            this.SetState(OrchestrationStep.CloseRetainerList, 15);
        }
    }

    private void ProcessSelectRetainer() {
        if (this.uiInteractionService.IsRetainerAvailable(this.currentRetainerName)) {
            if (this.uiInteractionService.SelectRetainer(this.currentRetainerName)) {
                this.SetState(OrchestrationStep.OpenMenu, 15);
            }
            else {
                this.Abort();
            }
        }
    }

    private void ProcessOpenMenu() {
        if (this.uiInteractionService.IsMenuReadyForRetainer(this.currentRetainerName)) {
            var optionText = this.currentTargetMenu == RetainerTargetMenu.MarketListings
                ? this.localizationService.Translate("RetainerMenu_SellItems")
                : this.localizationService.Translate("RetainerMenu_SalesHistory");

            if (this.uiInteractionService.IsMenuOptionAvailable(optionText)) {
                if (this.uiInteractionService.SelectMenuOption(optionText)) {
                    this.SetState(OrchestrationStep.WaitMenu, 30);
                }
                else {
                    this.Abort();
                }
            }
        }
    }

    private void ProcessWaitMenu() {
        var targetWindowName = this.currentTargetMenu == RetainerTargetMenu.MarketListings ? "RetainerSellList" : "RetainerHistory";
        var window = this.windowService.GetWindow(targetWindowName);

        if (window != null && window.IsVisible) {
            this.currentTask?.OnMenuOpened(this.currentRetainerName);
            this.SetState(OrchestrationStep.ExecutingTask, 15);
            this.timeoutTicks = int.MaxValue;
        }
    }

    private void ProcessExecutingTask() {
        if (this.currentTask != null) {
            bool isDone = this.currentTask.OnTick();
            if (isDone) {
                this.SetState(OrchestrationStep.CloseMenu, 15);
            }
        }
        else {
            this.SetState(OrchestrationStep.CloseMenu, 15);
        }
    }

    private void ProcessCloseMenu() {
        bool success = this.currentTargetMenu == RetainerTargetMenu.MarketListings
            ? this.uiInteractionService.CloseRetainerMarket()
            : this.uiInteractionService.CloseSalesHistory();

        if (success) {
            this.currentTask?.OnMenuClosed(this.currentRetainerName);
            this.SetState(OrchestrationStep.WaitMenuClosed, 30);
        }
    }

    private void ProcessWaitMenuClosed() {
        var targetWindowName = this.currentTargetMenu == RetainerTargetMenu.MarketListings ? "RetainerSellList" : "RetainerHistory";
        var window = this.windowService.GetWindow(targetWindowName);

        if (window == null || !window.IsVisible) {
            this.SetState(OrchestrationStep.CloseSelectString, 30);
        }
    }

    private void ProcessCloseSelectString() {
        if (this.uiInteractionService.IsMenuReadyForRetainer(this.currentRetainerName)) {
            if (this.uiInteractionService.CloseSelectString()) {
                this.AdvanceToNextRetainerOrFinish();
            }
        }
    }

    private void ProcessCloseRetainerList() {
        var window = this.windowService.GetWindow("RetainerList");
        if (window != null && window.IsVisible) {
            window.SendCallback(-1);
            this.Abort();
        }
    }

    private void SetState(OrchestrationStep nextStep, int initialCooldown) {
        this.currentStep = nextStep;
        this.cooldownTicks = initialCooldown;
        this.timeoutTicks = 600;
    }

    public void Dispose() {
        this.framework.Update -= this.OnFrameworkUpdate;
    }
}