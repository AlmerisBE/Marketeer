using Dalamud.Plugin.Services;
using Marketeer.API.Configuration.Contracts;
using Marketeer.API.Localization.Contracts;
using Marketeer.API.Logging.Contracts;
using Marketeer.API.RetainerAutomation.Contracts;
using Marketeer.API.RetainerAutomation.Models;
using Marketeer.API.RetainerAutomation.Services;
using Marketeer.API.UiInterop.Contracts;
using System;
using System.Collections.Generic;

namespace Marketeer.Core.RetainerAutomation.Services;

public class RetainerOrchestratorService : IRetainerOrchestratorService, IDisposable {
    private IFramework framework;
    private IRetainerUiInteractionService uiInteractionService;
    private INativeWindowService windowService;
    private ILocalizationService localizationService;
    private IConfigurationService configService;
    private ILoggerService logger;

    private Queue<string> retainerQueue;
    private string currentRetainerName;
    private RetainerTargetMenu currentTargetMenu;
    private IRetainerTask? currentTask;

    private OrchestrationStep currentStep;

    private DateTime actionAvailableAt;
    private DateTime timeoutAt;

    public bool IsActive { get; private set; }

    public RetainerOrchestratorService(
        IFramework framework,
        IRetainerUiInteractionService uiInteractionService,
        INativeWindowService windowService,
        ILocalizationService localizationService,
        IConfigurationService configService,
        ILoggerService logger) {

        this.framework = framework;
        this.uiInteractionService = uiInteractionService;
        this.windowService = windowService;
        this.localizationService = localizationService;
        this.configService = configService;
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

        if (DateTime.Now < this.actionAvailableAt) {
            return;
        }

        if (DateTime.Now > this.timeoutAt) {
            this.logger.Error($"Orchestration step {this.currentStep} timed out. Aborting.");
            this.Abort();
            return;
        }

        switch (this.currentStep) {
            case OrchestrationStep.SelectRetainer: this.ProcessSelectRetainer(); break;
            case OrchestrationStep.OpenMenu: this.ProcessOpenMenu(); break;
            case OrchestrationStep.WaitMenu: this.ProcessWaitMenu(); break;
            case OrchestrationStep.ExecutingTask: this.ProcessExecutingTask(); break;
            case OrchestrationStep.CloseMenu: this.ProcessCloseMenu(); break;
            case OrchestrationStep.WaitMenuClosed: this.ProcessWaitMenuClosed(); break;
            case OrchestrationStep.CloseSelectString: this.ProcessCloseSelectString(); break;
            case OrchestrationStep.CloseRetainerList: this.ProcessCloseRetainerList(); break;
        }
    }

    private void AdvanceToNextRetainerOrFinish() {
        if (this.retainerQueue.Count > 0) {
            this.currentRetainerName = this.retainerQueue.Dequeue();
            this.SetState(OrchestrationStep.SelectRetainer, 0.5 + this.GetRandomDelay());
        }
        else {
            this.SetState(OrchestrationStep.CloseRetainerList, 0.5);
        }
    }

    private void ProcessSelectRetainer() {
        if (this.uiInteractionService.IsRetainerAvailable(this.currentRetainerName)) {
            if (this.uiInteractionService.SelectRetainer(this.currentRetainerName)) {
                this.SetState(OrchestrationStep.OpenMenu, 0.5);
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
                    this.SetState(OrchestrationStep.WaitMenu, 1.0);
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
            this.SetState(OrchestrationStep.ExecutingTask, 0.5 + this.GetRandomDelay());
            this.timeoutAt = DateTime.MaxValue;
        }
    }

    private void ProcessExecutingTask() {
        if (this.currentTask != null) {
            bool isDone = this.currentTask.OnTick();
            if (isDone) {
                this.SetState(OrchestrationStep.CloseMenu, 0.5);
            }
        }
        else {
            this.SetState(OrchestrationStep.CloseMenu, 0.5);
        }
    }

    private void ProcessCloseMenu() {
        bool success = this.currentTargetMenu == RetainerTargetMenu.MarketListings
            ? this.uiInteractionService.CloseRetainerMarket()
            : this.uiInteractionService.CloseSalesHistory();

        if (success) {
            this.currentTask?.OnMenuClosed(this.currentRetainerName);
            this.SetState(OrchestrationStep.WaitMenuClosed, 1.0);
        }
    }

    private void ProcessWaitMenuClosed() {
        var targetWindowName = this.currentTargetMenu == RetainerTargetMenu.MarketListings ? "RetainerSellList" : "RetainerHistory";
        var window = this.windowService.GetWindow(targetWindowName);

        if (window == null || !window.IsVisible) {
            this.SetState(OrchestrationStep.CloseSelectString, 1.0);
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

    private void SetState(OrchestrationStep nextStep, double delaySeconds) {
        this.currentStep = nextStep;
        this.actionAvailableAt = DateTime.Now.AddSeconds(delaySeconds);
        this.timeoutAt = DateTime.Now.AddSeconds(20);
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

    public void Dispose() {
        this.framework.Update -= this.OnFrameworkUpdate;
    }
}