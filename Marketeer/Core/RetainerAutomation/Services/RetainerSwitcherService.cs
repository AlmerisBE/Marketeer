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

    public void SwitchTo(string retainerName, bool openMarketList = false) {
        this.targetRetainer = retainerName.Trim();
        this.shouldOpenMarketList = openMarketList;
        this.isSwitching = true;
        this.stateIndex = 0;
        this.nextActionAt = this.GetNow();
        this.timeoutAt = this.GetNow().AddSeconds(15);
        this.logger.Info($"Automated switch initiated for retainer: {this.targetRetainer}. Open market requested: {openMarketList}");
    }

    protected virtual DateTime GetNow() => DateTime.Now;

    private void OnFrameworkUpdate(IFramework fw) {
        if (!this.isSwitching) return;

        if (this.GetNow() > this.timeoutAt) {
            this.logger.Warning($"[RetainerSwitcherService] Sequence timed out for {this.targetRetainer}. Aborting.");
            this.isSwitching = false;
            this.uiInteraction.CloseUnexpectedWindows();
            return;
        }

        if (this.GetNow() < this.nextActionAt) return;

        this.uiInteraction.SkipDialogue();

        if (this.uiInteraction.IsAddonReady("SelectYesNo")) {
            this.uiInteraction.ConfirmYesNo();
            this.nextActionAt = this.GetNow().AddSeconds(0.5);
            return;
        }

        switch (this.stateIndex) {
            case 0:
                if (this.uiInteraction.IsAddonReady("RetainerSellList") || this.uiInteraction.IsAddonReady("RetainerHistory")) {
                    this.uiInteraction.CloseUnexpectedWindows();

                    bool closedMarket = this.uiInteraction.IsAddonReady("RetainerSellList") ? this.uiInteraction.CloseRetainerMarket() : true;
                    bool closedHistory = this.uiInteraction.IsAddonReady("RetainerHistory") ? this.uiInteraction.CloseSalesHistory() : true;

                    if (closedMarket && closedHistory) {
                        this.nextActionAt = this.GetNow().AddSeconds(0.5);
                        this.stateIndex = 1;
                    }
                }
                else this.stateIndex = 1;
                break;

            case 1:
                if (this.uiInteraction.IsAddonReady("SelectString")) {
                    if (this.uiInteraction.IsMenuReadyForRetainer(this.targetRetainer)) {
                        this.stateIndex = 4;
                        return;
                    }

                    if (this.uiInteraction.CloseSelectString()) {
                        this.nextActionAt = this.GetNow().AddSeconds(0.5);
                        this.stateIndex = 2;
                    }
                }
                else this.stateIndex = 2;
                break;

            case 2:
                if (this.uiInteraction.IsAddonReady("RetainerList")) {
                    this.nextActionAt = this.GetNow().AddSeconds(1.0);
                    this.stateIndex = 3;
                }
                break;

            case 3:
                if (this.uiInteraction.IsAddonReady("RetainerList")) {
                    if (this.uiInteraction.SelectRetainer(this.targetRetainer)) {
                        this.nextActionAt = this.GetNow().AddSeconds(1.5);
                        this.stateIndex = 4;
                    }
                }
                break;

            case 4:
                if (this.uiInteraction.IsAddonReady("SelectString")) {
                    if (this.shouldOpenMarketList) this.uiInteraction.OpenRetainerMarket();

                    this.logger.Info($"[RetainerSwitcherService] Successfully switched to {this.targetRetainer}. Sequence complete.");
                    this.isSwitching = false;
                    this.targetRetainer = string.Empty;
                }
                else if (this.uiInteraction.IsAddonReady("RetainerList")) {
                    this.logger.Warning($"[RetainerSwitcherService] Retainer click ignored by server. Retrying...");
                    this.stateIndex = 3;
                }
                break;
        }
    }

    public void Dispose() {
        this.framework.Update -= this.OnFrameworkUpdate;
    }
}