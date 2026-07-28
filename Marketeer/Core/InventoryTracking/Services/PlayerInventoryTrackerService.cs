using Dalamud.Plugin.Services;
using Marketeer.API.InventoryTracking.Contracts;
using Marketeer.API.Logging.Contracts;
using System;
using System.Linq;

namespace Marketeer.Core.InventoryTracking.Services;

public class PlayerInventoryTrackerService : IDisposable {
    private IFramework framework;
    private IClientState clientState;
    private IInventorySnapshotService snapshotService;
    private IInventoryDiffService diffService;
    private ILoggerService logger;

    private DateTime lastScanTime;
    private readonly TimeSpan scanInterval = TimeSpan.FromSeconds(5);

    public PlayerInventoryTrackerService(
        IFramework framework,
        IClientState clientState,
        IInventorySnapshotService snapshotService,
        IInventoryDiffService diffService,
        ILoggerService logger) {

        this.framework = framework;
        this.clientState = clientState;
        this.snapshotService = snapshotService;
        this.diffService = diffService;
        this.logger = logger;

        this.lastScanTime = DateTime.MinValue;
        this.framework.Update += this.OnFrameworkUpdate;
    }

    private void OnFrameworkUpdate(IFramework fw) {
        if (!this.clientState.IsLoggedIn) {
            return;
        }

        if (DateTime.Now - this.lastScanTime < this.scanInterval) {
            return;
        }

        this.lastScanTime = DateTime.Now;

        var newSnapshot = this.snapshotService.CreateSnapshot();
        if (newSnapshot.Items.Count == 0) {
            return; // Inventory not yet loaded in memory
        }

        var oldSnapshot = this.snapshotService.GetLatestSnapshot(newSnapshot.CharacterName, newSnapshot.HomeWorldId);

        if (oldSnapshot == null) {
            this.snapshotService.SaveSnapshot(newSnapshot);
            this.logger.Info("[PlayerInventoryTracker] Initial player inventory snapshot saved.");
            return;
        }

        var diff = this.diffService.Compare(oldSnapshot, newSnapshot);

        if (diff.Added.Any() || diff.Removed.Any() || diff.Moved.Any() || diff.QuantityChanged.Any()) {
            this.snapshotService.SaveSnapshot(newSnapshot);
            this.logger.Debug("[PlayerInventoryTracker] Player inventory changes detected and snapshot saved.");
        }
    }

    public void Dispose() {
        this.framework.Update -= this.OnFrameworkUpdate;
    }
}