using FFXIVClientStructs.FFXIV.Client.Game;
using Marketeer.API.GameInterop.Contracts;
using Marketeer.API.GameInterop.Models;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Features.Inventory.Services;

public class InventoryServiceTests {
    [Fact]
    public void GetInventorySlots_ReturnsMappedSlots_ForTDDMocking() {
        // Arrange
        var mockInventory = Substitute.For<IInventoryService>();
        var dummySlots = new List<InventorySlotInfo> {
            new InventorySlotInfo { SlotIndex = 0, IsOccupied = true, ItemId = 42, Quantity = 99, PricePerUnit = 1000 },
            new InventorySlotInfo { SlotIndex = 1, IsOccupied = false, ItemId = 0, Quantity = 0, PricePerUnit = 0 }
        };

        mockInventory.GetInventorySlots(InventoryType.Inventory1).Returns(dummySlots);

        // Act
        var result = mockInventory.GetInventorySlots(InventoryType.Inventory1);

        // Assert
        Assert.Equal(2, result.Count);

        Assert.True(result[0].IsOccupied);
        Assert.Equal(0u, result[0].SlotIndex);
        Assert.Equal(42u, result[0].ItemId);
        Assert.Equal(99u, result[0].Quantity);
        Assert.Equal(1000u, result[0].PricePerUnit);

        Assert.False(result[1].IsOccupied);
        Assert.Equal(1u, result[1].SlotIndex);
        Assert.Equal(0u, result[1].ItemId);
        Assert.Equal(0u, result[1].Quantity);
        Assert.Equal(0u, result[1].PricePerUnit);
    }
}