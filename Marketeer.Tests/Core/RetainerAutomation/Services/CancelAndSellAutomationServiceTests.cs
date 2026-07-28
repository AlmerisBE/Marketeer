using FFXIVClientStructs.FFXIV.Client.Game;
using Marketeer.API.GameInterop.Contracts;
using Marketeer.API.GameInterop.Models;
using Marketeer.API.Logging.Contracts;
using Marketeer.API.RetainerAutomation.Contracts;
using Marketeer.API.RetainerAutomation.Models;
using Marketeer.Core.RetainerAutomation.Services;
using NSubstitute;
using System.Reflection;
using Xunit;

namespace Marketeer.Tests.Core.RetainerAutomation.Services;

public class CancelAndSellAutomationServiceTests {
    [Fact]
    public void LocateTransferredItems_HandlesStackOverflowCorrectly() {
        // Arrange
        var mockOrchestrator = Substitute.For<IRetainerOrchestratorService>();
        var mockUi = Substitute.For<IRetainerUiInteractionService>();
        var mockInventory = Substitute.For<IInventoryService>();
        var mockLogger = Substitute.For<ILoggerService>();

        // We simulate returning 10 items to a bag that already has 990.
        // It should max out the first slot to 999 (+9), and overflow 1 to a new slot (+1).
        var oldSlots = new List<InventorySlotInfo> {
            new InventorySlotInfo { SlotIndex = 0, IsOccupied = true, ItemId = 123, Quantity = 990 },
            new InventorySlotInfo { SlotIndex = 1, IsOccupied = false }
        };

        var newSlots = new List<InventorySlotInfo> {
            new InventorySlotInfo { SlotIndex = 0, IsOccupied = true, ItemId = 123, Quantity = 999 },
            new InventorySlotInfo { SlotIndex = 1, IsOccupied = true, ItemId = 123, Quantity = 1 }
        };

        mockInventory.GetInventorySlots(InventoryType.RetainerPage1).Returns(oldSlots, newSlots);

        var service = new CancelAndSellAutomationService(mockOrchestrator, mockUi, mockInventory, mockLogger);

        // Act: We expect to transfer 10 items
        service.TriggerCancelAndSell(123, 10, 0);

        var methodInfo = typeof(CancelAndSellAutomationService).GetMethod("LocateTransferredItems", BindingFlags.NonPublic | BindingFlags.Instance);
        var result = (bool)methodInfo!.Invoke(service, null)!;

        // Assert
        Assert.True(result);

        var fieldInfo = typeof(CancelAndSellAutomationService).GetField("pendingSells", BindingFlags.NonPublic | BindingFlags.Instance);
        var queue = (Queue<TransferredItemStack>)fieldInfo!.GetValue(service)!;

        Assert.Equal(2, queue.Count);

        var firstStack = queue.Dequeue();
        Assert.Equal(0u, firstStack.SlotIndex);
        Assert.Equal(9u, firstStack.Quantity); // Added 9 to reach 999

        var secondStack = queue.Dequeue();
        Assert.Equal(1u, secondStack.SlotIndex);
        Assert.Equal(1u, secondStack.Quantity); // Remaining 1 went to next slot
    }
}