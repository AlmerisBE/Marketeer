using Dalamud.Plugin.Services;
using Marketeer.Core.Logging.Contracts;
using Marketeer.Core.RetainerAutomation.Contracts;
using System;

namespace Marketeer.Core.RetainerAutomation.Services;

public class RetainerSwitcherService : IRetainerSwitcherService, IDisposable {
    private IFramework framework;
    private IRetainerUiInteractionService uiInteraction;
    private ILoggerService logger;

    private string targetRetainer = string.Empty;
    private bool isSwitching;
    private bool shouldOpenMarketList;
    private DateTime nextActionAt;
    private DateTime timeoutAt;
    private int stateIndex;

    public RetainerSwitcherService(IFramework framework, IRetainerUiInteractionService uiInteraction, ILoggerService logger) {
        this.framework = framework;
        this.uiInteraction = uiInteraction;
        this.logger = logger;
        this.framework.Update += this.OnFrameworkUpdate;
    }

    // Accessible for TDD time manipulation
    protected virtual DateTime GetNow() => DateTime.Now;

    public void SwitchTo(string retainerName, bool openMarketList = false) {
        this.targetRetainer = retainerName.Trim();
        this.shouldOpenMarketList = openMarketList;
        this.isSwitching = true;
        this.stateIndex = 0;
        this.nextActionAt = DateTime.MinValue; // Start immediately
        this.timeoutAt = this.GetNow().AddSeconds(15);
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
            if (this.GetNow() >= this.nextActionAt) {
                this.uiInteraction.ConfirmYesNo();
                this.nextActionAt = this.GetNow().AddSeconds(0.5); // Throttle click spam
            }
            return; // Wait for it to close natively
        }

        switch (this.stateIndex) {
            case 0:
                bool marketReady = this.uiInteraction.IsAddonReady("RetainerSellList");
                bool historyReady = this.uiInteraction.IsAddonReady("RetainerHistory");

                if (!marketReady && !historyReady) {
                    this.stateIndex = 1;
                    this.nextActionAt = DateTime.MinValue; // Instantly advance
                }
                else if (this.GetNow() >= this.nextActionAt) {
                    if (marketReady) this.uiInteraction.CloseRetainerMarket();
                    if (historyReady) this.uiInteraction.CloseSalesHistory();
                    this.nextActionAt = this.GetNow().AddSeconds(0.5); // Throttle close attempts
                }
                break;

            case 1:
                if (this.uiInteraction.IsAddonReady("SelectString")) {
                    if (this.uiInteraction.IsMenuReadyForRetainer(this.targetRetainer)) {
                        this.stateIndex = 4; // Already correct retainer
                        this.nextActionAt = DateTime.MinValue;
                    }
                    else if (this.GetNow() >= this.nextActionAt) {
                        this.uiInteraction.CloseSelectString();
                        this.nextActionAt = this.GetNow().AddSeconds(0.5);
                    }
                }
                else if (this.uiInteraction.IsAddonReady("RetainerList")) {
                    this.stateIndex = 2;
                    this.nextActionAt = DateTime.MinValue;
                }
                break;

            case 2:
                if (this.uiInteraction.IsAddonReady("RetainerList")) {
                    if (this.GetNow() >= this.nextActionAt) {
                        this.uiInteraction.SelectRetainer(this.targetRetainer);
                        this.stateIndex = 3; // Move to passive waiting state
                        this.nextActionAt = this.GetNow().AddSeconds(2.0); // Wait up to 2s for server transition
                    }
                }
                break;

            case 3:
                if (this.uiInteraction.IsAddonReady("SelectString")) {
                    this.stateIndex = 4;
                    this.nextActionAt = DateTime.MinValue;
                }
                else if (this.uiInteraction.IsAddonReady("RetainerList") && this.GetNow() >= this.nextActionAt) {
                    // Transition failed (e.g. packet loss), let's retry the click
                    this.stateIndex = 2;
                    this.nextActionAt = DateTime.MinValue;
                }
                break;

            case 4:
                if (this.uiInteraction.IsAddonReady("SelectString")) {
                    if (this.uiInteraction.IsMenuReadyForRetainer(this.targetRetainer)) {
                        if (this.shouldOpenMarketList) {
                            if (this.GetNow() >= this.nextActionAt) {
                                this.uiInteraction.OpenRetainerMarket();
                                this.logger.Info($"[RetainerSwitcherService] Switched and opened market for {this.targetRetainer}.");
                                this.isSwitching = false;
                            }
                        }
                        else {
                            this.logger.Info($"[RetainerSwitcherService] Switched to {this.targetRetainer}.");
                            this.isSwitching = false;
                        }
                    }
                    else {
                        // The wrong retainer was summoned (packet drop or spam click prevention). Go back and retry.
                        this.stateIndex = 1;
                        this.nextActionAt = DateTime.MinValue;
                    }
                }
                else if (this.uiInteraction.IsAddonReady("RetainerList")) {
                    this.stateIndex = 2; // Kicked back to the list natively
                    this.nextActionAt = DateTime.MinValue;
                }
                break;
        }
    }

    public void Dispose() {
        this.framework.Update -= this.OnFrameworkUpdate;
    }
}