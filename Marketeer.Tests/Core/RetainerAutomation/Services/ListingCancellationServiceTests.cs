using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using Marketeer.API.GameInterop.Contracts;
using Marketeer.API.GameInterop.Models;
using Marketeer.Core.Logging.Contracts;
using Marketeer.Core.RetainerAutomation.Contracts;
using Marketeer.Core.RetainerAutomation.Services;
using Marketeer.UI.Localization.Contracts;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Core.RetainerAutomation.Services;

public class ListingCancellationServiceTests {
    [Fact]
    public void TriggerCancellation_ShouldActivateService_WhenItemIsFound() {
        var uiInteraction = Substitute.For<IRetainerUiInteractionService>();
        var inventoryService = Substitute.For<IInventoryService>();
        var localization = Substitute.For<ILocalizationService>();
        var framework = Substitute.For<IFramework>();
        var logger = Substitute.For<ILoggerService>();

        var slots = new List<InventorySlotInfo> {
            new InventorySlotInfo { ItemId = 100, SlotIndex = 0, Quantity = 1, IsOccupied = true }
        };
        inventoryService.GetInventorySlots(InventoryType.RetainerMarket).Returns(slots);
        inventoryService.GetUiIndexForRetainerMarketItem(0).Returns(0);

        using var service = new ListingCancellationService(uiInteraction, inventoryService, localization, framework, logger);

        service.TriggerCancellation(100);

        Assert.True(service.IsActive);
    }

    [Fact]
    public void TriggerCancellation_ShouldNotActivate_WhenItemNotFound() {
        var uiInteraction = Substitute.For<IRetainerUiInteractionService>();
        var inventoryService = Substitute.For<IInventoryService>();
        var localization = Substitute.For<ILocalizationService>();
        var framework = Substitute.For<IFramework>();
        var logger = Substitute.For<ILoggerService>();

        var slots = new List<InventorySlotInfo>();
        inventoryService.GetInventorySlots(InventoryType.RetainerMarket).Returns(slots);

        using var service = new ListingCancellationService(uiInteraction, inventoryService, localization, framework, logger);

        service.TriggerCancellation(100);

        Assert.False(service.IsActive);
        logger.Received(1).Warning(Arg.Is<string>(s => s.Contains("Could not find item ID")));
    }
}