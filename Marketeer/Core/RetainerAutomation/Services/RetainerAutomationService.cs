using Marketeer.API.GameInterop.Contracts;
using Marketeer.API.Logging.Contracts;
using Marketeer.API.MarketListings.Contracts;
using Marketeer.API.RetainerAutomation.Contracts;
using Marketeer.API.RetainerAutomation.Models;
using System.Linq;

namespace Marketeer.Core.RetainerAutomation.Services;

public class RetainerAutomationService : IRetainerAutomationService, IRetainerTask {
    private IRetainerOrchestratorService orchestrator;
    private IRetainerProvider retainerProvider;
    private IMarketListingTrackerService marketListingTracker;
    private ILoggerService logger;

    private int waitTicks;
    private ulong currentRetainerId;

    public bool IsScanning => this.orchestrator.IsActive;

    public RetainerAutomationService(
        IRetainerOrchestratorService orchestrator,
        IRetainerProvider retainerProvider,
        IMarketListingTrackerService marketListingTracker,
        ILoggerService logger) {

        this.orchestrator = orchestrator;
        this.retainerProvider = retainerProvider;
        this.marketListingTracker = marketListingTracker;
        this.logger = logger;
    }

    public void TriggerScan() {
        if (this.IsScanning) {
            return;
        }

        var activeRetainers = this.retainerProvider.GetActiveRetainers();

        if (activeRetainers.Count == 0) {
            this.logger.Warning("No active retainers found to scan.");
            return;
        }

        var names = activeRetainers.Select(r => r.Name).ToList();
        this.orchestrator.StartOrchestration(names, RetainerTargetMenu.MarketListings, this);
    }

    public void AbortScan() {
        this.orchestrator.Abort();
    }

    public void OnMenuOpened(string retainerName) {
        var retainer = this.retainerProvider.GetActiveRetainers().FirstOrDefault(r => r.Name == retainerName);
        this.currentRetainerId = retainer?.RetainerId ?? 0;

        this.waitTicks = 15;
    }

    public bool OnTick() {
        if (this.waitTicks > 0) {
            this.waitTicks--;
            return false;
        }

        if (this.currentRetainerId != 0) {
            this.marketListingTracker.ScanListings(this.currentRetainerId, true);
        }

        return true;
    }

    public void OnMenuClosed(string retainerName) {
        // Aucune action nécessaire
    }

    public void OnAbort() {
        // Aucune action nécessaire
    }
}