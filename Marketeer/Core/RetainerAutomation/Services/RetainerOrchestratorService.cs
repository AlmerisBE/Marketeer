using Dalamud.Plugin.Services;
using Marketeer.API.GameInterop.Contracts;
using Marketeer.API.UiInterop.Contracts;
using Marketeer.Core.Configuration.Contracts;
using Marketeer.Core.Logging.Contracts;
using Marketeer.Core.RetainerAutomation.Contracts;
using Marketeer.Core.RetainerAutomation.Models;
using Marketeer.UI.Localization.Contracts;
using System;
using System.Collections.Generic;

namespace Marketeer.Core.RetainerAutomation.Services;

public class RetainerOrchestratorService : IRetainerOrchestratorService, IDisposable {
    private const double RetainerAvailabilityDelay = 5.0;

    private IFramework framework;
    private IRetainerUiInteractionService uiInteractionService;
    private INativeWindowService windowService;
    private ILocalizationService localizationService;
    private IConfigurationService configService;
    private ILoggerService logger;
    private IWorldInteractionService worldInteractionService;

    private Queue<string> retainerQueue;
    private string currentRetainerName;
    private RetainerTargetMenu currentTargetMenu;
    private IRetainerTask? currentTask;

    private OrchestrationStep currentStep;

    private DateTime actionAvailableAt;
    private DateTime timeoutAt;
    private DateTime stepEnteredAt;

    public bool IsActive { get; private set; }

    public RetainerOrchestratorService(
        IFramework framework,
        IRetainerUiInteractionService uiInteractionService,
        INativeWindowService windowService,
        ILocalizationService localizationService,
        IConfigurationService configService,
        ILoggerService logger,
        IWorldInteractionService worldInteractionService) {

        this.framework = framework;
        this.uiInteractionService = uiInteractionService;
        this.windowService = windowService;
        this.localizationService = localizationService;
        this.configService = configService;
        this.logger = logger;
        this.worldInteractionService = worldInteractionService;

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
        if (this.retainerQueue.Count == 0) return;

        this.currentTargetMenu = targetMenu;
        this.currentTask = task;
        this.IsActive = true;
        this.logger.Info($"Starting Retainer Orchestration for {this.retainerQueue.Count} retainers. Target Menu: {targetMenu}.");

        if (!this.uiInteractionService.IsAddonReady("RetainerList")) this.SetState(OrchestrationStep.OpenBell, 0);
        else this.AdvanceToNextRetainerOrFinish();
    }

    public void Abort() {
        this.IsActive = false;
        this.currentStep = OrchestrationStep.Idle;
        this.currentTask?.OnAbort();
        this.currentTask = null;
        this.logger.Info("Retainer orchestration sequence aborted/concluded.");
    }

    private void OnFrameworkUpdate(IFramework frameworkInstance) {
        if (!this.IsActive) return;

        // Dialogue skipping runs continuously before any delays to clear 'Talk' addons instantly
        this.uiInteractionService.SkipDialogue();

        if (DateTime.Now < this.actionAvailableAt) return;

        if (DateTime.Now > this.timeoutAt) {
            this.logger.Error($"Orchestration step {this.currentStep} timed out. Aborting.");
            this.uiInteractionService.CloseUnexpectedWindows();
            this.Abort();
            return;
        }

        switch (this.currentStep) {
            case OrchestrationStep.OpenBell: this.ProcessOpenBell(); break;
            case OrchestrationStep.WaitBell: this.ProcessWaitBell(); break;
            case OrchestrationStep.SelectRetainer: this.ProcessSelectRetainer(); break;
            case OrchestrationStep.WaitSelectStringOpen: this.ProcessWaitSelectStringOpen(); break;
            case OrchestrationStep.OpenMenu: this.ProcessOpenMenu(); break;
            case OrchestrationStep.WaitMenu: this.ProcessWaitMenu(); break;
            case OrchestrationStep.ExecutingTask: this.ProcessExecutingTask(); break;
            case OrchestrationStep.CloseMenu: this.ProcessCloseMenu(); break;
            case OrchestrationStep.WaitMenuClosed: this.ProcessWaitMenuClosed(); break;
            case OrchestrationStep.WaitSelectStringReturn: this.ProcessWaitSelectStringReturn(); break;
            case OrchestrationStep.CloseSelectString: this.ProcessCloseSelectString(); break;
            case OrchestrationStep.WaitRetainerListReturn: this.ProcessWaitRetainerListReturn(); break;
            case OrchestrationStep.CloseRetainerList: this.ProcessCloseRetainerList(); break;
        }
    }

    private void ProcessOpenBell() {
        if (this.worldInteractionService.InteractWithSummoningBell()) {
            this.SetState(OrchestrationStep.WaitBell, 0);
        }
        else {
            this.logger.Warning("Cannot start orchestration: Summoning bell not in range and menu not open.");
            this.Abort();
        }
    }

    private void ProcessWaitBell() {
        if (this.uiInteractionService.IsAddonReady("RetainerList")) this.AdvanceToNextRetainerOrFinish();
    }

    private void AdvanceToNextRetainerOrFinish() {
        if (this.retainerQueue.Count > 0) {
            this.currentRetainerName = this.retainerQueue.Dequeue();
            this.SetState(OrchestrationStep.SelectRetainer, 1.0 + this.GetRandomDelay());
        }
        else this.SetState(OrchestrationStep.CloseRetainerList, 0.5 + this.GetRandomDelay());
    }

    private void ProcessSelectRetainer() {
        if (this.uiInteractionService.IsRetainerAvailable(this.currentRetainerName)) {
            if (this.uiInteractionService.SelectRetainer(this.currentRetainerName)) {
                this.SetState(OrchestrationStep.WaitSelectStringOpen, 0);
                return;
            }
        }

        // Extended grace period to accommodate network lag or heavy server load
        if ((DateTime.Now - this.stepEnteredAt).TotalSeconds > RetainerAvailabilityDelay) {
            this.logger.Warning($"Skipping retainer {this.currentRetainerName} as it is not available.");
            this.AdvanceToNextRetainerOrFinish();
        }
    }

    private void ProcessWaitSelectStringOpen() {
        if (this.uiInteractionService.IsMenuReadyForRetainer(this.currentRetainerName)) this.SetState(OrchestrationStep.OpenMenu, 0.5 + this.GetRandomDelay());
    }

    private void ProcessOpenMenu() {
        var optionText = this.currentTargetMenu == RetainerTargetMenu.MarketListings
            ? this.localizationService.Translate("RetainerMenu_SellItems")
            : this.localizationService.Translate("RetainerMenu_SalesHistory");

        if (this.uiInteractionService.IsMenuOptionAvailable(optionText)) {
            if (this.uiInteractionService.SelectMenuOption(optionText)) {
                this.SetState(OrchestrationStep.WaitMenu, 0);
                return;
            }
        }

        if ((DateTime.Now - this.stepEnteredAt).TotalSeconds > RetainerAvailabilityDelay) {
            this.logger.Warning($"Skipping interaction, menu option '{optionText}' not available.");
            this.AdvanceToNextRetainerOrFinish();
        }
    }

    private void ProcessWaitMenu() {
        var targetWindowName = this.currentTargetMenu == RetainerTargetMenu.MarketListings ? "RetainerSellList" : "RetainerHistory";

        if (this.uiInteractionService.IsAddonReady(targetWindowName)) {
            this.currentTask?.OnMenuOpened(this.currentRetainerName);
            this.SetState(OrchestrationStep.ExecutingTask, 0.5 + this.GetRandomDelay());
            this.timeoutAt = DateTime.MaxValue;
        }
    }

    private void ProcessExecutingTask() {
        if (this.currentTask != null) {
            if (this.currentTask.OnTick()) this.SetState(OrchestrationStep.CloseMenu, 0.5 + this.GetRandomDelay());
        }
        else this.SetState(OrchestrationStep.CloseMenu, 0);
    }

    private void ProcessCloseMenu() {
        bool success = this.currentTargetMenu == RetainerTargetMenu.MarketListings
            ? this.uiInteractionService.CloseRetainerMarket()
            : this.uiInteractionService.CloseSalesHistory();

        if (success) {
            this.currentTask?.OnMenuClosed(this.currentRetainerName);
            this.SetState(OrchestrationStep.WaitMenuClosed, 0);
        }
        else this.AdvanceToNextRetainerOrFinish();
    }

    private void ProcessWaitMenuClosed() {
        var targetWindowName = this.currentTargetMenu == RetainerTargetMenu.MarketListings ? "RetainerSellList" : "RetainerHistory";
        var window = this.windowService.GetWindow(targetWindowName);

        if (window == null || !window.IsVisible) this.SetState(OrchestrationStep.WaitSelectStringReturn, 0);
    }

    private void ProcessWaitSelectStringReturn() {
        if (this.uiInteractionService.IsAddonReady("SelectString")) this.SetState(OrchestrationStep.CloseSelectString, 0.5 + this.GetRandomDelay());
    }

    private void ProcessCloseSelectString() {
        if (this.uiInteractionService.CloseSelectString()) this.SetState(OrchestrationStep.WaitRetainerListReturn, 0);
        else this.AdvanceToNextRetainerOrFinish();
    }

    private void ProcessWaitRetainerListReturn() {
        if (this.uiInteractionService.IsAddonReady("RetainerList")) this.AdvanceToNextRetainerOrFinish();
    }

    private void ProcessCloseRetainerList() {
        var window = this.windowService.GetWindow("RetainerList");
        if (window != null && window.IsVisible) window.SendCallback(-1);

        this.Abort();
    }

    private void SetState(OrchestrationStep nextStep, double delaySeconds) {
        this.currentStep = nextStep;
        this.stepEnteredAt = DateTime.Now;
        this.actionAvailableAt = DateTime.Now.AddSeconds(delaySeconds);
        this.timeoutAt = DateTime.Now.AddSeconds(20);
    }

    private double GetRandomDelay() {
        var config = this.configService.GetConfig();
        if (!config.EnableAutomationDelay) return 0;

        var min = config.AutomationDelayMin;
        var max = config.AutomationDelayMax;
        if (min > max) min = max;

        return min + (new Random().NextDouble() * (max - min));
    }

    public void Dispose() {
        this.framework.Update -= this.OnFrameworkUpdate;
    }
}