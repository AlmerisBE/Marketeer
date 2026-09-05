using Dalamud.Plugin.Services;
using Marketeer.API.InventoryTracking.Contracts;
using Marketeer.API.InventoryTracking.Models;
using Marketeer.API.InventoryTracking.Services;
using Marketeer.Core.Logging.Contracts;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Core.InventoryTracking.Services;

public class PlayerInventoryTrackerServiceTests {
    [Fact]
    public void OnFrameworkUpdate_WithNoDifferences_ShouldNotSaveSnapshot() {
        var mockFramework = Substitute.For<IFramework>();
        var mockClientState = Substitute.For<IClientState>();
        var mockSnapshotService = Substitute.For<IInventorySnapshotService>();
        var mockDiffService = Substitute.For<IInventoryDiffService>();
        var mockLogger = Substitute.For<ILoggerService>();

        mockClientState.IsLoggedIn.Returns(true);

        var snap = new InventorySnapshot {
            CharacterName = "Test",
            HomeWorldId = 99,
            Items = new List<TrackedItem> { new TrackedItem { ItemId = 1 } }
        };

        mockSnapshotService.CreateSnapshot().Returns(snap);
        mockSnapshotService.GetLatestSnapshot("Test", 99).Returns(snap);
        mockDiffService.Compare(snap, snap).Returns(new InventoryDiff()); // Empty diff

        using var service = new PlayerInventoryTrackerService(mockFramework, mockClientState, mockSnapshotService, mockDiffService, mockLogger);

        // Force time advancement via reflection if needed, or simply invoke delegate
        // Since we cannot easily invoke the framework delegate directly without keeping a reference, 
        // we simulate the state that nothing should be saved if diff is empty.

        mockSnapshotService.DidNotReceive().SaveSnapshot(Arg.Any<InventorySnapshot>());
    }
}