using Marketeer.Features.Dashboard.Contracts;
using Marketeer.Features.Logging.Contracts;
using Marketeer.Features.RetainerAutomation.Contracts;
using Marketeer.Features.RetainerOrchestration.Contracts;
using Marketeer.Features.RetainerOrchestration.Models;
using Marketeer.Features.RetainerTracking.Contracts;
using System.Linq;

namespace Marketeer.Features.RetainerAutomation.Services;

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

        // Attendre que l'interface se stabilise avant de scanner
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

        return true; // Tâche terminée pour ce servant
    }

    public void OnMenuClosed(string retainerName) {
        // Aucune action nécessaire
    }

    public void OnAbort() {
        // Aucune action nécessaire
    }
}