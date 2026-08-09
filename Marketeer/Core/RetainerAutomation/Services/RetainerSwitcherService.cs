using Dalamud.Plugin.Services;
using Marketeer.API.Logging.Contracts;
using Marketeer.API.RetainerAutomation.Contracts;
using System;

namespace Marketeer.Core.RetainerAutomation.Services;

public class RetainerSwitcherService : IRetainerSwitcherService, IDisposable {
    private IFramework framework;
    private IRetainerUiInteractionService uiInteraction;
    private ILoggerService logger;

    private string targetRetainer = string.Empty;
    private bool isSwitching;
    private DateTime nextActionAt;

    public RetainerSwitcherService(IFramework framework, IRetainerUiInteractionService uiInteraction, ILoggerService logger) {
        this.framework = framework;
        this.uiInteraction = uiInteraction;
        this.logger = logger;
        this.framework.Update += this.OnFrameworkUpdate;
    }

    public void SwitchTo(string retainerName) {
        if (this.isSwitching) {
            return;
        }

        this.targetRetainer = retainerName;
        this.isSwitching = true;
        this.nextActionAt = this.GetNow();
        this.logger.Info($"Automated switch initiated for retainer: {retainerName}");
    }

    protected virtual DateTime GetNow() => DateTime.Now;

    private void OnFrameworkUpdate(IFramework fw) {
        if (!this.isSwitching || this.GetNow() < this.nextActionAt) {
            return;
        }

        this.uiInteraction.SkipDialogue();

        if (this.uiInteraction.IsAddonReady("RetainerSellList") || this.uiInteraction.IsAddonReady("RetainerHistory")) {
            this.uiInteraction.CloseUnexpectedWindows();
            this.uiInteraction.CloseRetainerMarket();
            this.uiInteraction.CloseSalesHistory();
            this.nextActionAt = this.GetNow().AddSeconds(0.5);
        }
        else if (this.uiInteraction.IsAddonReady("SelectString")) {
            this.uiInteraction.CloseSelectString();
            this.nextActionAt = this.GetNow().AddSeconds(0.5);
        }
        else if (this.uiInteraction.IsAddonReady("RetainerList")) {
            this.uiInteraction.SelectRetainer(this.targetRetainer);
            this.isSwitching = false;
            this.targetRetainer = string.Empty;
        }
    }

    public void Dispose() {
        this.framework.Update -= this.OnFrameworkUpdate;
    }
}