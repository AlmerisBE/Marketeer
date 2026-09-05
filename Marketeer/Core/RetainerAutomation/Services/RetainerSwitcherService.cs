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
    private bool targetRetainerSelected;
    private bool shouldOpenMarketList;
    private DateTime nextActionAt;

    public RetainerSwitcherService(IFramework framework, IRetainerUiInteractionService uiInteraction, ILoggerService logger) {
        this.framework = framework;
        this.uiInteraction = uiInteraction;
        this.logger = logger;
        this.framework.Update += this.OnFrameworkUpdate;
    }

    public void SwitchTo(string retainerName, bool openMarketList = false) {
        if (this.isSwitching) {
            return;
        }

        this.targetRetainer = retainerName;
        this.shouldOpenMarketList = openMarketList;
        this.isSwitching = true;
        this.targetRetainerSelected = false;
        this.nextActionAt = this.GetNow();
        this.logger.Info($"Automated switch initiated for retainer: {retainerName}. Open market requested: {openMarketList}");
    }

    protected virtual DateTime GetNow() => DateTime.Now;

    private void OnFrameworkUpdate(IFramework fw) {
        if (!this.isSwitching || this.GetNow() < this.nextActionAt) {
            return;
        }

        this.uiInteraction.SkipDialogue();

        // Intercept and auto-confirm the buyback warning dialog if it appears during the switch process
        if (this.uiInteraction.IsAddonReady("SelectYesNo")) {
            this.uiInteraction.ConfirmYesNo();
            this.nextActionAt = this.GetNow().AddSeconds(0.5);
            return;
        }

        if (this.uiInteraction.IsAddonReady("RetainerSellList") || this.uiInteraction.IsAddonReady("RetainerHistory")) {
            this.uiInteraction.CloseUnexpectedWindows();

            bool closedMarket = this.uiInteraction.IsAddonReady("RetainerSellList") ? this.uiInteraction.CloseRetainerMarket() : true;
            bool closedHistory = this.uiInteraction.IsAddonReady("RetainerHistory") ? this.uiInteraction.CloseSalesHistory() : true;

            if (closedMarket && closedHistory) {
                this.nextActionAt = this.GetNow().AddSeconds(0.5);
            }
        }
        else if (!this.targetRetainerSelected && this.uiInteraction.IsAddonReady("SelectString")) {
            if (this.uiInteraction.CloseSelectString()) {
                this.nextActionAt = this.GetNow().AddSeconds(0.5);
            }
        }
        else if (!this.targetRetainerSelected && this.uiInteraction.IsAddonReady("RetainerList")) {
            if (this.uiInteraction.SelectRetainer(this.targetRetainer)) {
                this.targetRetainerSelected = true;
                this.nextActionAt = this.GetNow().AddSeconds(0.5);
            }

        }
        else if (this.targetRetainerSelected && this.uiInteraction.IsAddonReady("SelectString")) {
            if (this.shouldOpenMarketList) {
                this.uiInteraction.OpenRetainerMarket();
            }

            this.isSwitching = false;
            this.targetRetainer = string.Empty;
        }
    }

    public void Dispose() {
        this.framework.Update -= this.OnFrameworkUpdate;
    }
}