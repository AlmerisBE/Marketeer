using Dalamud.Plugin.Services;
using Marketeer.API.GameInterop.Contracts;
using Marketeer.API.InventoryTracking.Contracts;
using Marketeer.API.Logging.Contracts;
using Marketeer.API.UiInterop.Contracts;
using System;
using System.Linq;

namespace Marketeer.Core.InventoryTracking.Services;

public class RetainerInventoryTrackerService : IDisposable {
    private IFramework framework;
    private IMarketListingProvider listingProvider;
    private IRetainerProvider retainerProvider;
    private IInventorySnapshotService snapshotService;
    private INativeWindowService windowService;
    private ILoggerService logger;

    private ulong lastScannedRetainerId = 0;

    public RetainerInventoryTrackerService(
        IFramework framework,
        IMarketListingProvider listingProvider,
        IRetainerProvider retainerProvider,
        IInventorySnapshotService snapshotService,
        INativeWindowService windowService,
        ILoggerService logger) {

        this.framework = framework;
        this.listingProvider = listingProvider;
        this.retainerProvider = retainerProvider;
        this.snapshotService = snapshotService;
        this.windowService = windowService;
        this.logger = logger;

        this.framework.Update += this.OnFrameworkUpdate;
    }

    private void OnFrameworkUpdate(IFramework fw) {
        var activeId = this.listingProvider.GetActiveRetainerId();

        if (activeId.HasValue && activeId.Value != 0) {
            if (this.lastScannedRetainerId != activeId.Value) {
                // Wait for the server to transmit the inventory data.
                // The "SelectString" menu only appears once the retainer memory is fully loaded.
                var menuWindow = this.windowService.GetWindow("SelectString");

                if (menuWindow != null && menuWindow.IsVisible) {
                    var retainers = this.retainerProvider.GetActiveRetainers();
                    var retainer = retainers.FirstOrDefault(r => r.RetainerId == activeId.Value);

                    if (retainer != null) {
                        var snapshot = this.snapshotService.CreateRetainerSnapshot(retainer.RetainerId, retainer.Name);

                        if (snapshot.Items.Count > 0) {
                            this.snapshotService.SaveRetainerSnapshot(retainer.RetainerId, snapshot);
                            this.lastScannedRetainerId = activeId.Value;
                            this.logger.Info($"[RetainerInventoryTracker] Successfully captured inventory snapshot for retainer: {retainer.Name}");
                        }
                    }
                }
            }
        }
        else {
            this.lastScannedRetainerId = 0;
        }
    }

    public void Dispose() {
        this.framework.Update -= this.OnFrameworkUpdate;
    }
}