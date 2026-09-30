using Dalamud.Plugin.Services;
using Marketeer.Core.Logging.Contracts;
using Marketeer.Core.RetainerAutomation.Contracts;
using System;
using System.Collections.Generic;

namespace Marketeer.Core.RetainerAutomation.Services;

public class RetainerSwitcherService : IRetainerSwitcherService, IDisposable {
    private IFramework framework;
    private IRetainerUiInteractionService uiInteraction;
    private ILoggerService logger;

    private string targetRetainer = string.Empty;
    private bool isSwitching;
    private bool shouldOpenMarketList;
    private bool hasReachedTargetMenu;
    private DateTime timeoutAt;

    private Dictionary<string, DateTime> throttles = new();

    public RetainerSwitcherService(IFramework framework, IRetainerUiInteractionService uiInteraction, ILoggerService logger) {
        this.framework = framework;
        this.uiInteraction = uiInteraction;
        this.logger = logger;
        this.framework.Update += this.OnFrameworkUpdate;
    }

    // Accessible for TDD time manipulation
    protected virtual DateTime GetNow() => DateTime.UtcNow;

    private bool Throttle(string key, int cooldownMs = 500) {
        var now = this.GetNow();
        if (!this.throttles.TryGetValue(key, out var lastTime) || (now - lastTime).TotalMilliseconds > cooldownMs) {
            this.throttles[key] = now;
            return true;
        }
        return false;
    }

    public void SwitchTo(string retainerName, bool openMarketList = false) {
        this.targetRetainer = retainerName.Trim();
        this.shouldOpenMarketList = openMarketList;
        this.isSwitching = true;
        this.hasReachedTargetMenu = false;
        this.timeoutAt = this.GetNow().AddSeconds(15);
        this.throttles.Clear();

        this.logger.Info($"Automated switch initiated for retainer: {this.targetRetainer}. Open market requested: {openMarketList}");
    }

    private void OnFrameworkUpdate(IFramework fw) {
        if (!this.isSwitching) return;

        if (this.GetNow() > this.timeoutAt) {
            this.logger.Warning($"[RetainerSwitcherService] Sequence timed out for {this.targetRetainer}. Aborting.");
            this.isSwitching = false;
            this.uiInteraction.CloseUnexpectedWindows();
            return;
        }

        this.uiInteraction.SkipDialogue();

        if (this.uiInteraction.IsAddonReady("SelectYesNo")) {
            if (this.Throttle("SelectYesNo", 500)) this.uiInteraction.ConfirmYesNo();
            return;
        }

        // 1. If Market or History is open
        if (this.uiInteraction.IsAddonReady("RetainerSellList") || this.uiInteraction.IsAddonReady("RetainerHistory")) {
            if (this.hasReachedTargetMenu) {
                this.isSwitching = false; // Goal reached
                return;
            }

            if (this.Throttle("CloseSubMenu", 500)) {
                if (this.uiInteraction.IsAddonReady("RetainerSellList")) this.uiInteraction.CloseRetainerMarket();
                if (this.uiInteraction.IsAddonReady("RetainerHistory")) this.uiInteraction.CloseSalesHistory();
            }
            return;
        }

        // 2. If SelectString is open
        if (this.uiInteraction.IsAddonReady("SelectString")) {
            if (this.uiInteraction.IsMenuReadyForRetainer(this.targetRetainer)) {
                if (this.shouldOpenMarketList) {
                    if (this.Throttle("OpenMarketList", 500)) {
                        this.hasReachedTargetMenu = true;
                        this.uiInteraction.OpenRetainerMarket();
                        this.logger.Info($"[RetainerSwitcherService] Opened market for {this.targetRetainer}.");
                    }
                }
                else {
                    this.logger.Info($"[RetainerSwitcherService] Switched to {this.targetRetainer}.");
                    this.isSwitching = false;
                }
            }
            else {
                if (this.Throttle("CloseSelectString", 500)) {
                    this.uiInteraction.CloseSelectString();
                }
            }
            return;
        }

        // 3. If RetainerList is open
        if (this.uiInteraction.IsAddonReady("RetainerList")) {
            if (this.Throttle("SelectRetainer", 1000)) { // 1 second throttle to allow server to summon
                this.uiInteraction.SelectRetainer(this.targetRetainer);
            }
            return;
        }
    }

    public void Dispose() {
        this.framework.Update -= this.OnFrameworkUpdate;
    }
}