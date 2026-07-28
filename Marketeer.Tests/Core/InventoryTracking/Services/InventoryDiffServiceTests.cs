using Marketeer.API.InventoryTracking.Models;
using Marketeer.Core.InventoryTracking.Services;
using Xunit;

namespace Marketeer.Tests.Core.InventoryTracking.Services;

public class InventoryDiffServiceTests {
    private InventoryDiffService diffService;

    public InventoryDiffServiceTests() {
        this.diffService = new InventoryDiffService();
    }

    [Fact]
    public void Compare_WhenItemQuantityChanges_ShouldReportQuantityChange() {
        var oldSnap = new InventorySnapshot {
            Items = new List<TrackedItem> { new TrackedItem { ItemId = 123, Quantity = 10, ContainerId = 1, SlotIndex = 0 } }
        };
        var newSnap = new InventorySnapshot {
            Items = new List<TrackedItem> { new TrackedItem { ItemId = 123, Quantity = 15, ContainerId = 1, SlotIndex = 0 } }
        };

        var diff = this.diffService.Compare(oldSnap, newSnap);

        Assert.Single(diff.QuantityChanged);
        Assert.Equal(5, diff.QuantityChanged.First().Difference);
        Assert.Empty(diff.Added);
        Assert.Empty(diff.Removed);
        Assert.Empty(diff.Moved);
    }

    [Fact]
    public void Compare_WhenItemMoves_ShouldReportMove() {
        var oldSnap = new InventorySnapshot {
            Items = new List<TrackedItem> { new TrackedItem { ItemId = 123, Quantity = 10, ContainerId = 1, SlotIndex = 0 } }
        };
        var newSnap = new InventorySnapshot {
            Items = new List<TrackedItem> { new TrackedItem { ItemId = 123, Quantity = 10, ContainerId = 2, SlotIndex = 5 } }
        };

        var diff = this.diffService.Compare(oldSnap, newSnap);

        Assert.Single(diff.Moved);
        Assert.Equal(1u, diff.Moved.First().OldContainerId);
        Assert.Equal(2u, diff.Moved.First().NewContainerId);
        Assert.Empty(diff.Added);
        Assert.Empty(diff.Removed);
        Assert.Empty(diff.QuantityChanged);
    }

    [Fact]
    public void Compare_WhenItemReplaced_ShouldReportRemoveAndAdd() {
        var oldSnap = new InventorySnapshot {
            Items = new List<TrackedItem> { new TrackedItem { ItemId = 123, Quantity = 1, ContainerId = 1, SlotIndex = 0 } }
        };
        var newSnap = new InventorySnapshot {
            Items = new List<TrackedItem> { new TrackedItem { ItemId = 999, Quantity = 1, ContainerId = 1, SlotIndex = 0 } }
        };

        var diff = this.diffService.Compare(oldSnap, newSnap);

        Assert.Single(diff.Removed);
        Assert.Equal(123u, diff.Removed.First().ItemId);
        Assert.Single(diff.Added);
        Assert.Equal(999u, diff.Added.First().ItemId);
        Assert.Empty(diff.Moved);
        Assert.Empty(diff.QuantityChanged);
    }
}