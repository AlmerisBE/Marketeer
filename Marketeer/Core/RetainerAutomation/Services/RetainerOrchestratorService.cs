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
    private IFramework framework;
    private IRetainerUiInteractionService uiInteraction;
    private INativeWindowService windowService;
    private ILocalizationService localization;
    private IConfigurationService configService;
    private ILoggerService logger;
    private IWorldInteractionService worldInteraction;

    private Queue<string> retainerQueue = new();
    private string currentRetainer = string.Empty;
    private RetainerTargetMenu currentTargetMenu;
    private IRetainerTask? currentTask;

    private bool hasReachedTargetMenu;
    private DateTime timeoutAt;
    private Dictionary<string, DateTime> throttles = new();

    public bool IsActive { get; private set; }

    public RetainerOrchestratorService(
        IFramework framework,
        IRetainerUiInteractionService uiInteraction,
        INativeWindowService windowService,
        ILocalizationService localization,
        IConfigurationService configService,
        ILoggerService logger,
        IWorldInteractionService worldInteraction) {

        this.framework = framework;
        this.uiInteraction = uiInteraction;
        this.windowService = windowService;
        this.localization = localization;
        this.configService = configService;
        this.logger = logger;
        this.worldInteraction = worldInteraction;

        this.framework.Update += this.OnFrameworkUpdate;
    }

    public void StartOrchestration(IEnumerable<string> retainerNames, RetainerTargetMenu targetMenu, IRetainerTask task) {
        if (this.IsActive) return;

        this.retainerQueue = new Queue<string>(retainerNames);
        if (this.retainerQueue.Count == 0) return;

        this.currentTargetMenu = targetMenu;
        this.currentTask = task;
        this.currentRetainer = string.Empty;
        this.hasReachedTargetMenu = false;
        this.timeoutAt = DateTime.UtcNow.AddSeconds(20);
        this.throttles.Clear();

        this.IsActive = true;
        this.logger.Info($"Starting Orchestration for {this.retainerQueue.Count} retainers.");
    }

    public void Abort() {
        this.IsActive = false;
        this.currentTask?.OnAbort();
        this.currentTask = null;
        this.logger.Info("Retainer orchestration aborted/concluded.");
    }

    private int GetThrottleMs(int baseMs) {
        var config = this.configService.GetConfig();
        if (!config.EnableAutomationDelay) return baseMs;

        var min = config.AutomationDelayMin * 1000;
        var max = config.AutomationDelayMax * 1000;
        if (min > max) min = max;

        return baseMs + new Random().Next(min, max);
    }

    private bool Throttle(string key, int baseCooldownMs = 500) {
        var now = DateTime.UtcNow;
        if (!this.throttles.TryGetValue(key, out var lastTime) || (now - lastTime).TotalMilliseconds > baseCooldownMs) {
            this.throttles[key] = now;
            return true;
        }
        return false;
    }

    private void OnFrameworkUpdate(IFramework fw) {
        if (!this.IsActive) return;

        this.uiInteraction.SkipDialogue();

        if (DateTime.UtcNow > this.timeoutAt) {
            this.logger.Error($"Orchestration timed out on retainer '{this.currentRetainer}'. Aborting.");
            this.uiInteraction.CloseUnexpectedWindows();
            this.Abort();
            return;
        }

        if (this.uiInteraction.IsAddonReady("SelectYesNo")) {
            if (this.Throttle("SelectYesNo", this.GetThrottleMs(500))) this.uiInteraction.ConfirmYesNo();
            return;
        }

        var targetWindowName = this.currentTargetMenu == RetainerTargetMenu.MarketListings ? "RetainerSellList" : "RetainerHistory";

        if (this.uiInteraction.IsAddonReady(targetWindowName)) {
            if (this.hasReachedTargetMenu && this.currentTask != null) {
                if (this.Throttle("TickTask", 100)) {
                    if (this.currentTask.OnTick()) {
                        if (this.Throttle("CloseTaskMenu", this.GetThrottleMs(500))) {
                            if (this.currentTargetMenu == RetainerTargetMenu.MarketListings) this.uiInteraction.CloseRetainerMarket();
                            else this.uiInteraction.CloseSalesHistory();

                            this.hasReachedTargetMenu = false;
                            this.currentTask.OnMenuClosed(this.currentRetainer);
                            this.currentRetainer = string.Empty; // Force moving to the next retainer natively
                        }
                    }
                }
                return;
            }

            if (this.Throttle("CloseSubMenu", this.GetThrottleMs(500))) {
                if (this.uiInteraction.IsAddonReady("RetainerSellList")) this.uiInteraction.CloseRetainerMarket();
                if (this.uiInteraction.IsAddonReady("RetainerHistory")) this.uiInteraction.CloseSalesHistory();
            }
            return;
        }

        if (this.uiInteraction.IsAddonReady("SelectString")) {
            if (!string.IsNullOrEmpty(this.currentRetainer) && this.uiInteraction.IsMenuReadyForRetainer(this.currentRetainer)) {
                var optionText = this.currentTargetMenu == RetainerTargetMenu.MarketListings
                    ? this.localization.Translate("RetainerMenu_SellItems")
                    : this.localization.Translate("RetainerMenu_SalesHistory");

                if (this.uiInteraction.IsMenuOptionAvailable(optionText)) {
                    if (this.Throttle("OpenTargetMenu", this.GetThrottleMs(500))) {
                        this.uiInteraction.SelectMenuOption(optionText);
                        this.hasReachedTargetMenu = true;
                        this.currentTask?.OnMenuOpened(this.currentRetainer);
                        this.timeoutAt = DateTime.MaxValue; // Suspend timeout while task operates
                    }
                }
                else {
                    if (this.Throttle("CloseSelectString", this.GetThrottleMs(500))) {
                        this.logger.Warning($"Option not available. Skipping {this.currentRetainer}.");
                        this.uiInteraction.CloseSelectString();
                        this.currentRetainer = string.Empty; // Force next retainer
                    }
                }
            }
            else {
                if (this.Throttle("CloseSelectString", this.GetThrottleMs(500))) {
                    this.uiInteraction.CloseSelectString();
                }
            }
            return;
        }

        if (this.uiInteraction.IsAddonReady("RetainerList")) {
            if (this.retainerQueue.Count == 0 && string.IsNullOrEmpty(this.currentRetainer)) {
                if (this.Throttle("CloseRetainerList", this.GetThrottleMs(500))) {
                    var window = this.windowService.GetWindow("RetainerList");
                    if (window != null && window.IsVisible) window.SendCallback(-1);
                    this.Abort();
                }
            }
            else {
                if (string.IsNullOrEmpty(this.currentRetainer)) {
                    this.currentRetainer = this.retainerQueue.Dequeue();
                    this.timeoutAt = DateTime.UtcNow.AddSeconds(20); // Reset timeout for new retainer
                }

                if (this.uiInteraction.IsRetainerAvailable(this.currentRetainer)) {
                    if (this.Throttle("SelectRetainer", this.GetThrottleMs(1000))) {
                        this.uiInteraction.SelectRetainer(this.currentRetainer);
                    }
                }
                else {
                    this.logger.Warning($"Retainer {this.currentRetainer} not found. Skipping.");
                    this.currentRetainer = string.Empty;
                }
            }
            return;
        }

        if (this.Throttle("InteractBell", 2000)) {
            if (!this.worldInteraction.InteractWithSummoningBell()) {
                this.logger.Warning("Cannot orchestrate: Bell not in range.");
                this.Abort();
            }
        }
    }

    public void Dispose() {
        this.framework.Update -= this.OnFrameworkUpdate;
    }
}