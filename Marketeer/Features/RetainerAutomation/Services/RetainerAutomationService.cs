using Dalamud.Plugin.Services;
using Marketeer.Features.Logging.Contracts;
using Marketeer.Features.RetainerAutomation.Contracts;
using System;

namespace Marketeer.Features.RetainerAutomation.Services;

public enum AutomationStep {
    SelectRetainer,
    OpenMarketListings,
    CloseMarketListings,
    CloseSelectString
}

public class RetainerAutomationService : IRetainerAutomationService, IDisposable {
    private IUiInteractionService uiInteraction;
    private IClientRetainerService retainerService;
    private IFramework framework;
    private ILoggerService logger;
    private int currentRetainerIndex;
    private int cooldownTicks;
    private AutomationStep currentStep;

    public bool IsScanning { get; private set; }

    public RetainerAutomationService(
        IUiInteractionService uiInteraction,
        IClientRetainerService retainerService,
        IFramework framework,
        ILoggerService logger) {
        this.uiInteraction = uiInteraction;
        this.retainerService = retainerService;
        this.framework = framework;
        this.logger = logger;
        this.framework.Update += this.OnFrameworkUpdate;
    }

    public void TriggerScan() {
        var activeRetainers = this.retainerService.GetActiveRetainerCount();
        if (activeRetainers == 0) {
            this.logger.Warning("RetainerAutomationService: No active retainers detected. Aborting sequence.");
            return;
        }

        this.logger.Debug($"RetainerAutomationService: TriggerScan invoked for {activeRetainers} retainers.");
        this.currentRetainerIndex = 0;
        this.cooldownTicks = 15;
        this.currentStep = AutomationStep.SelectRetainer;
        this.IsScanning = true;
    }

    public void Reset() {
        this.logger.Debug("RetainerAutomationService: Scan sequence completed or aborted.");
        this.IsScanning = false;
        this.currentRetainerIndex = 0;
        this.currentStep = AutomationStep.SelectRetainer;
    }

    private void OnFrameworkUpdate(IFramework frameworkInstance) {
        if (!this.IsScanning) {
            return;
        }

        if (this.cooldownTicks > 0) {
            this.cooldownTicks--;
            return;
        }

        switch (this.currentStep) {
            case AutomationStep.SelectRetainer:
                if (this.uiInteraction.IsAddonReady("RetainerList")) {
                    this.uiInteraction.SelectRetainer(this.currentRetainerIndex);
                    this.currentStep = AutomationStep.OpenMarketListings;
                    this.cooldownTicks = 45;
                }
                break;

            case AutomationStep.OpenMarketListings:
                if (this.uiInteraction.IsAddonReady("SelectString")) {
                    this.uiInteraction.OpenRetainerMarket();
                    this.currentStep = AutomationStep.CloseMarketListings;
                    this.cooldownTicks = 60; // Extra ticks to allow item data to fetch from the server
                }
                break;

            case AutomationStep.CloseMarketListings:
                if (this.uiInteraction.IsAddonReady("RetainerSell")) {
                    this.uiInteraction.CloseRetainerMarket();
                    this.currentStep = AutomationStep.CloseSelectString;
                    this.cooldownTicks = 30;
                }
                break;

            case AutomationStep.CloseSelectString:
                if (this.uiInteraction.IsAddonReady("SelectString")) {
                    this.uiInteraction.CloseSelectString();
                    this.currentRetainerIndex++;
                    this.currentStep = AutomationStep.SelectRetainer;
                    this.cooldownTicks = 45;

                    if (this.currentRetainerIndex >= this.retainerService.GetActiveRetainerCount()) {
                        this.Reset();
                    }
                }
                break;
        }
    }

    public void Dispose() {
        this.framework.Update -= this.OnFrameworkUpdate;
    }
}