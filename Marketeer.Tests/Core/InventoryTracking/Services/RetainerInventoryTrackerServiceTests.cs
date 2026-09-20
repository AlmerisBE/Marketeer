using Dalamud.Plugin.Services;
using Marketeer.API.GameInterop.Contracts;
using Marketeer.API.InventoryTracking.Contracts;
using Marketeer.API.InventoryTracking.Models;
using Marketeer.API.InventoryTracking.Services;
using Marketeer.Core.CharacterManagement.Models;
using Marketeer.Core.Logging.Contracts;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Core.InventoryTracking.Services;

public class RetainerInventoryTrackerServiceTests {
    [Fact]
    public void OnFrameworkUpdate_WhenInventorySnapshotIsEmpty_ShouldNotSaveSnapshot() {
        var framework = Substitute.For<IFramework>();
        var listingProvider = Substitute.For<IMarketListingProvider>();
        var retainerProvider = Substitute.For<IRetainerProvider>();
        var snapshotService = Substitute.For<IInventorySnapshotService>();
        var diffService = Substitute.For<IInventoryDiffService>();
        var logger = Substitute.For<ILoggerService>();

        listingProvider.GetActiveRetainerId().Returns(12345ul);

        var retainers = new List<TrackedRetainer> {
            new TrackedRetainer { RetainerId = 12345ul, Name = "Adelaide" }
        };
        retainerProvider.GetActiveRetainers().Returns(retainers);

        // Simulate a ghost read where items have not yet loaded from the server
        var emptySnapshot = new InventorySnapshot { Items = new List<TrackedItem>() };
        snapshotService.CreateRetainerSnapshot(12345ul, "Adelaide").Returns(emptySnapshot);

        using var service = new RetainerInventoryTrackerService(
            framework, listingProvider, retainerProvider,
            snapshotService, diffService, logger);

        framework.Update += Raise.Event<IFramework.OnUpdateDelegate>(framework);

        snapshotService.DidNotReceiveWithAnyArgs().SaveRetainerSnapshot(default, default!);
    }

    [Fact]
    public void OnFrameworkUpdate_WhenInventoryHasItemsAndDiffExists_ShouldSaveSnapshot() {
        var framework = Substitute.For<IFramework>();
        var listingProvider = Substitute.For<IMarketListingProvider>();
        var retainerProvider = Substitute.For<IRetainerProvider>();
        var snapshotService = Substitute.For<IInventorySnapshotService>();
        var diffService = Substitute.For<IInventoryDiffService>();
        var logger = Substitute.For<ILoggerService>();

        listingProvider.GetActiveRetainerId().Returns(12345ul);

        var retainers = new List<TrackedRetainer> {
            new TrackedRetainer { RetainerId = 12345ul, Name = "Adelaide" }
        };
        retainerProvider.GetActiveRetainers().Returns(retainers);

        var oldSnapshot = new InventorySnapshot();
        var newSnapshot = new InventorySnapshot { Items = new List<TrackedItem> { new TrackedItem() } };

        snapshotService.GetLatestRetainerSnapshot(12345ul).Returns(oldSnapshot);
        snapshotService.CreateRetainerSnapshot(12345ul, "Adelaide").Returns(newSnapshot);

        var diff = new InventoryDiff { Added = new List<TrackedItem> { new TrackedItem() } };
        diffService.Compare(oldSnapshot, newSnapshot).Returns(diff);

        using var service = new RetainerInventoryTrackerService(
            framework, listingProvider, retainerProvider,
            snapshotService, diffService, logger);

        framework.Update += Raise.Event<IFramework.OnUpdateDelegate>(framework);

        snapshotService.Received(1).SaveRetainerSnapshot(12345ul, newSnapshot);
    }
}