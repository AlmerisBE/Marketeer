using Dalamud.Plugin.Services;
using Marketeer.Core.Logging.Contracts;
using Marketeer.Core.MarketListings.Contracts;
using Marketeer.Core.MarketListings.Models;
using Marketeer.Core.MarketPricing.Contracts;
using Marketeer.Core.RetainerAutomation.Contracts;
using Marketeer.Core.RetainerAutomation.Services;
using Marketeer.UI.Localization.Contracts;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Core.RetainerAutomation.Services;

public class HybridAutomationServiceTests {
    [Fact]
    public async Task StartPriceUpdate_ShouldTraverseStateMachineAndOpenComparePrices() {
        var framework = Substitute.For<IFramework>();
        var uiInteraction = Substitute.For<IRetainerUiInteractionService>();
        var priceProvider = Substitute.For<IMarketPriceCacheService>();
        var objectTable = Substitute.For<IObjectTable>();
        var localization = Substitute.For<ILocalizationService>();
        var logger = Substitute.For<ILoggerService>();
        var listingTracker = Substitute.For<IMarketListingTrackerService>();
        var guidanceService = Substitute.For<IRetainerGuidanceService>();
        var priceCalculationService = Substitute.For<IPriceCalculationService>();
        var notificationService = Substitute.For<INotificationService>();
        var delayProvider = Substitute.For<IAutomationDelayProvider>();

        localization.Translate(Arg.Any<string>()).Returns("Adjust Price");
        uiInteraction.GetContextMenuItemIndex("Adjust Price").Returns(2);

        using var service = new HybridAutomationService(
            framework, uiInteraction, priceProvider, objectTable, localization,
            logger, listingTracker, guidanceService, priceCalculationService, notificationService, delayProvider);

        var listing = new TrackedListing { ItemId = 100u, ItemName = "Test Item" };

        // Act - Start automation (State 0 initialization)
        service.StartPriceUpdate(listing);
        Assert.True(service.IsActive);

        // Advance past initial 0.1s delay to trigger State 0
        await Task.Delay(150);
        service.EvaluateTick();

        // Assert State 0 execution
        uiInteraction.Received(1).SelectContextMenuItem(2);

        // Advance past 0.2s delay and simulate RetainerSell window opening to trigger State 1
        await Task.Delay(250);
        uiInteraction.IsAddonReady("RetainerSell").Returns(true);
        service.EvaluateTick();

        // Assert State 1 execution
        uiInteraction.Received(1).OpenComparePrices();
    }

    [Fact]
    public async Task StartNewSale_ShouldBypassContextMenuAndTraverseToComparePrices() {
        var framework = Substitute.For<IFramework>();
        var uiInteraction = Substitute.For<IRetainerUiInteractionService>();
        var priceProvider = Substitute.For<IMarketPriceCacheService>();
        var objectTable = Substitute.For<IObjectTable>();
        var localization = Substitute.For<ILocalizationService>();
        var logger = Substitute.For<ILoggerService>();
        var listingTracker = Substitute.For<IMarketListingTrackerService>();
        var guidanceService = Substitute.For<IRetainerGuidanceService>();
        var priceCalculationService = Substitute.For<IPriceCalculationService>();
        var notificationService = Substitute.For<INotificationService>();
        var delayProvider = Substitute.For<IAutomationDelayProvider>();

        using var service = new HybridAutomationService(
            framework, uiInteraction, priceProvider, objectTable, localization,
            logger, listingTracker, guidanceService, priceCalculationService, notificationService, delayProvider);

        // Act - Start new sale automation (State 1 initialization)
        service.StartNewSale(100u, "Test Item");
        Assert.True(service.IsActive);

        // Advance past 0.2s delay and simulate RetainerSell window opening directly
        await Task.Delay(250);
        uiInteraction.IsAddonReady("RetainerSell").Returns(true);
        service.EvaluateTick();

        // Assert State 1 execution (Directly compares prices without context menu interaction)
        uiInteraction.Received(1).OpenComparePrices();
    }
}