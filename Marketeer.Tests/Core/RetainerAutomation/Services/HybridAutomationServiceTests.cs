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
    public void StartPriceUpdate_ShouldActivateServiceAndOpenComparePrices() {
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

        using var service = new HybridAutomationService(
            framework, uiInteraction, priceProvider, objectTable, localization,
            logger, listingTracker, guidanceService, priceCalculationService, notificationService);

        var listing = new TrackedListing { ItemId = 100u, ItemName = "Test Item" };
        nint dummyAddonAddress = 123456;

        service.StartPriceUpdate(listing, dummyAddonAddress);

        Assert.True(service.IsActive);
        uiInteraction.Received(1).OpenComparePrices(dummyAddonAddress);
    }
}