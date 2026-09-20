using Dalamud.Plugin.Services;
using Marketeer.API.GameInterop.Contracts;
using Marketeer.API.InventoryTracking.Contracts;
using Marketeer.Core.Logging.Contracts;
using System;
using System.Linq;

namespace Marketeer.API.InventoryTracking.Services;

public class RetainerInventoryTrackerService : IDisposable {
    private IFramework framework;
    private IMarketListingProvider listingProvider;
    private IRetainerProvider retainerProvider;
    private IInventorySnapshotService snapshotService;
    private IInventoryDiffService diffService;
    private ILoggerService logger;
    private IGameEventService gameEventService;

    private DateTime lastScanTime;
    private readonly TimeSpan scanInterval = TimeSpan.FromSeconds(2);
    private bool isTracking = false;

    public RetainerInventoryTrackerService(
        IFramework framework,
        IMarketListingProvider listingProvider,
        IRetainerProvider retainerProvider,
        IInventorySnapshotService snapshotService,
        IInventoryDiffService diffService,
        ILoggerService logger,
        IGameEventService gameEventService) {

        this.framework = framework;
        this.listingProvider = listingProvider;
        this.retainerProvider = retainerProvider;
        this.snapshotService = snapshotService;
        this.diffService = diffService;
        this.logger = logger;
        this.gameEventService = gameEventService;

        this.lastScanTime = DateTime.MinValue;

        this.gameEventService.RetainerSessionStarted += this.StartTracking;
        this.gameEventService.RetainerSessionEnded += this.StopTracking;
    }

    private void StartTracking() {
        if (this.isTracking) return;

        this.isTracking = true;
        this.lastScanTime = DateTime.MinValue;
        this.framework.Update += this.OnFrameworkUpdate;

        this.logger.Debug("[RetainerInventoryTracker] Session started. Resuming inventory monitoring.");
    }

    private void StopTracking() {
        if (!this.isTracking) return;

        this.isTracking = false;
        this.framework.Update -= this.OnFrameworkUpdate;

        this.logger.Debug("[RetainerInventoryTracker] Session ended. Pausing inventory monitoring.");
    }

    private void OnFrameworkUpdate(IFramework fw) {
        if (DateTime.Now - this.lastScanTime < this.scanInterval) return;

        this.lastScanTime = DateTime.Now;

        var activeId = this.listingProvider.GetActiveRetainerId();
        if (!activeId.HasValue || activeId.Value == 0) return;

        var retainers = this.retainerProvider.GetActiveRetainers();
        var retainer = retainers.FirstOrDefault(r => r.RetainerId == activeId.Value);

        if (retainer == null) return;

        var newSnapshot = this.snapshotService.CreateRetainerSnapshot(retainer.RetainerId, retainer.Name);

        if (newSnapshot.Items.Count == 0) return;

        var oldSnapshot = this.snapshotService.GetLatestRetainerSnapshot(retainer.RetainerId);

        if (oldSnapshot == null) {
            this.snapshotService.SaveRetainerSnapshot(retainer.RetainerId, newSnapshot);
            this.logger.Info($"[RetainerInventoryTracker] Initial inventory snapshot saved for retainer: {retainer.Name}");
            return;
        }

        var diff = this.diffService.Compare(oldSnapshot, newSnapshot);
        if (diff.Added.Any() || diff.Removed.Any() || diff.Moved.Any() || diff.QuantityChanged.Any()) {
            this.snapshotService.SaveRetainerSnapshot(retainer.RetainerId, newSnapshot);
            this.logger.Info($"[RetainerInventoryTracker] Inventory changes detected and snapshot updated for retainer: {retainer.Name}");
        }
    }

    public void Dispose() {
        this.gameEventService.RetainerSessionStarted -= this.StartTracking;
        this.gameEventService.RetainerSessionEnded -= this.StopTracking;
        this.StopTracking();
    }
}