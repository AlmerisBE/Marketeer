using Dalamud.Plugin.Services;
using Marketeer.Features.Dashboard.Contracts;
using Marketeer.Features.Localization.Contracts;
using Marketeer.Features.Logging.Contracts;
using Marketeer.Features.RetainerAutomation.Contracts;
using Marketeer.Features.Retainers.Contracts;
using Marketeer.Features.RetainerTracking.Contracts;
using Marketeer.Features.RetainerTracking.Models;
using Marketeer.Features.WindowAbstraction.Contracts;
using System;
using System.Collections.Generic;

namespace Marketeer.Features.RetainerAutomation.Services;

public class RetainerAutomationService : IRetainerAutomationService, IDisposable {
    private IFramework framework;
    private IRetainerService retainerService;
    private IRetainerProvider retainerProvider;
    private INativeWindowService windowService;
    private ILocalizationService localizationService;
    private IMarketListingTrackerService marketListingTracker;
    private ILoggerService logger;

    private IReadOnlyList<TrackedRetainer> activeRetainers;
    private int currentRetainerIndex;
    private int cooldownTicks;
    private int timeoutTicks;
    private AutomationStep currentStep;
    private bool isFirstScan;

    public bool IsScanning { get; private set; }

    public RetainerAutomationService(
        IFramework framework,
        IRetainerService retainerService,
        IRetainerProvider retainerProvider,
        INativeWindowService windowService,
        ILocalizationService localizationService,
        IMarketListingTrackerService marketListingTracker,
        ILoggerService logger) {

        this.framework = framework;
        this.retainerService = retainerService;
        this.retainerProvider = retainerProvider;
        this.windowService = windowService;
        this.localizationService = localizationService;
        this.marketListingTracker = marketListingTracker;
        this.logger = logger;

        this.activeRetainers = new List<TrackedRetainer>();
        this.currentStep = AutomationStep.Idle;

        this.framework.Update += this.OnFrameworkUpdate;
    }

    public void TriggerScan() {
        if (this.IsScanning) {
            return;
        }

        this.activeRetainers = this.retainerProvider.GetActiveRetainers();

        if (this.activeRetainers.Count == 0) {
            this.logger.Warning("No active retainers found to scan.");
            return;
        }

        this.logger.Info($"Starting automated scan for {this.activeRetainers.Count} retainers.");
        this.currentRetainerIndex = 0;
        this.cooldownTicks = 15;
        this.timeoutTicks = 600;
        this.IsScanning = true;
        this.currentStep = AutomationStep.SelectRetainer;
    }

    public void AbortScan() {
        this.IsScanning = false;
        this.currentStep = AutomationStep.Idle;
        this.logger.Info("Automated retainer scan sequence concluded.");
    }

    private void OnFrameworkUpdate(IFramework frameworkInstance) {
        if (!this.IsScanning) {
            return;
        }

        if (this.cooldownTicks > 0) {
            this.cooldownTicks--;
            return;
        }

        if (this.timeoutTicks <= 0) {
            this.logger.Error($"Automation step {this.currentStep} timed out. Aborting scan.");
            this.AbortScan();
            return;
        }
        this.timeoutTicks--;

        switch (this.currentStep) {
            case AutomationStep.SelectRetainer:
                this.ProcessSelectRetainer();
                break;
            case AutomationStep.OpenMarketListings:
                this.ProcessOpenMarketListings();
                break;
            case AutomationStep.ScanMarketListings:
                this.ProcessScanMarketListings();
                break;
            case AutomationStep.WaitAndCloseMarketListings:
                this.ProcessCloseMarketListings();
                break;
            case AutomationStep.CloseSelectString:
                this.ProcessCloseSelectString();
                break;
            case AutomationStep.CloseRetainerList:
                this.ProcessCloseRetainerList();
                break;
        }
    }

    private void ProcessSelectRetainer() {
        var targetName = this.activeRetainers[this.currentRetainerIndex].Name;

        if (this.retainerService.IsRetainerAvailable(targetName)) {
            var success = this.retainerService.SelectRetainer(targetName);

            if (success) {
                // Initialize the first scan flag immediately upon summoning the retainer
                this.isFirstScan = true;
                this.currentStep = AutomationStep.OpenMarketListings;
                this.cooldownTicks = 15;
                this.timeoutTicks = 600;
            }
            else {
                this.logger.Error($"Failed to select retainer {targetName}. Aborting scan.");
                this.AbortScan();
            }
        }
    }

    private void ProcessOpenMarketListings() {
        var targetName = this.activeRetainers[this.currentRetainerIndex].Name;
        var optionText = this.localizationService.Translate("RetainerMenu_SellItems");

        if (this.retainerService.IsMenuReadyForRetainer(targetName) && this.retainerService.IsMenuOptionAvailable(optionText)) {
            var success = this.retainerService.SelectMenuOption(optionText);

            if (success) {
                this.currentStep = AutomationStep.ScanMarketListings;
                this.cooldownTicks = 30; // Wait for UI to load before attempting to scan
                this.timeoutTicks = 600;
            }
            else {
                this.logger.Error($"Failed to find menu option '{optionText}'. Aborting scan.");
                this.AbortScan();
            }
        }
    }

    private void ProcessScanMarketListings() {
        var retainerId = this.activeRetainers[this.currentRetainerIndex].RetainerId;

        // Perform the scan, passing down the contextual isFirstScan state
        bool scanSuccessful = this.marketListingTracker.ScanListings(retainerId, this.isFirstScan);

        if (scanSuccessful) {
            this.logger.Info($"Market listings scanned for retainer ID {retainerId}. First scan was: {this.isFirstScan}.");

            // Guarantee that any continuous polling while the retainer is summoned flags as subsequent scans
            this.isFirstScan = false;

            this.currentStep = AutomationStep.WaitAndCloseMarketListings;
            this.cooldownTicks = 15;
            this.timeoutTicks = 600;
        }
    }

    private void ProcessCloseMarketListings() {
        if (this.retainerService.CloseMarketListings()) {
            this.currentStep = AutomationStep.CloseSelectString;
            this.cooldownTicks = 60; // Allow time for the server to acknowledge closure and SelectString to reopen
            this.timeoutTicks = 600;
        }
    }

    private void ProcessCloseSelectString() {
        var targetName = this.activeRetainers[this.currentRetainerIndex].Name;

        if (this.retainerService.IsMenuReadyForRetainer(targetName)) {
            if (this.retainerService.CloseRetainerMenu()) {
                this.currentRetainerIndex++;
                this.cooldownTicks = 60;
                this.timeoutTicks = 600;

                if (this.currentRetainerIndex >= this.activeRetainers.Count) {
                    this.currentStep = AutomationStep.CloseRetainerList;
                }
                else {
                    this.currentStep = AutomationStep.SelectRetainer;
                }
            }
        }
    }

    private void ProcessCloseRetainerList() {
        var window = this.windowService.GetWindow("RetainerList");

        if (window != null && window.IsVisible) {
            window.SendCallback(-1);
            this.logger.Info("Retainer scan complete. Closed RetainerList.");
            this.AbortScan();
        }
    }

    public void Dispose() {
        this.framework.Update -= this.OnFrameworkUpdate;
    }
}