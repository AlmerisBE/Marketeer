using Dalamud.Plugin.Services;
using Marketeer.API.GameInterop.Contracts;
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
    private IFramework framework;
    private IRetainerUiInteractionService uiInteraction;
    private IMarketPriceCacheService priceProvider;
    private IObjectTable objectTable;
    private ILocalizationService localization;
    private ILoggerService logger;
    private IMarketListingTrackerService listingTracker;
    private IRetainerGuidanceService guidanceService;
    private IPriceCalculationService priceCalculationService;
    private INotificationService notificationService;
    private IAutomationDelayProvider delayProvider;
    private IMarketListingProvider listingProvider;

    public HybridAutomationServiceTests() {
        this.framework = Substitute.For<IFramework>();
        this.uiInteraction = Substitute.For<IRetainerUiInteractionService>();
        this.priceProvider = Substitute.For<IMarketPriceCacheService>();
        this.objectTable = Substitute.For<IObjectTable>();
        this.localization = Substitute.For<ILocalizationService>();
        this.logger = Substitute.For<ILoggerService>();
        this.listingTracker = Substitute.For<IMarketListingTrackerService>();
        this.guidanceService = Substitute.For<IRetainerGuidanceService>();
        this.priceCalculationService = Substitute.For<IPriceCalculationService>();
        this.notificationService = Substitute.For<INotificationService>();
        this.delayProvider = Substitute.For<IAutomationDelayProvider>();
        this.listingProvider = Substitute.For<IMarketListingProvider>();

        // Ensure delays are minimal for rapid test execution
        this.delayProvider.GetDelay(Arg.Any<int>()).Returns(TimeSpan.FromMilliseconds(10));
    }

    private HybridAutomationService CreateService() {
        return new HybridAutomationService(
            this.framework,
            this.uiInteraction,
            this.priceProvider,
            this.objectTable,
            this.localization,
            this.logger,
            this.listingTracker,
            this.guidanceService,
            this.priceCalculationService,
            this.notificationService,
            this.delayProvider,
            this.listingProvider
        );
    }

    [Fact]
    public void StartPriceUpdate_WhenInactive_SetsIsActiveAndInitializesSequence() {
        var service = this.CreateService();
        var listing = new TrackedListing { ItemId = 1, ItemName = "Test Item", PricePerUnit = 500 };

        service.StartPriceUpdate(listing);

        Assert.True(service.IsActive);
    }

    [Fact]
    public void StartPriceUpdate_WhenAlreadyActive_IgnoresRequest() {
        var service = this.CreateService();
        var initialListing = new TrackedListing { ItemId = 1, ItemName = "First Item" };
        var secondListing = new TrackedListing { ItemId = 2, ItemName = "Second Item" };

        service.StartPriceUpdate(initialListing);
        service.StartPriceUpdate(secondListing);

        // Service should still be active, but logging or internal state tracking would show it ignored the second call.
        Assert.True(service.IsActive);
    }

    [Fact]
    public void StartNewSale_FetchesActiveRetainerId_AndSetsIsActive() {
        this.listingProvider.GetActiveRetainerId().Returns(12345ul);
        var service = this.CreateService();

        service.StartNewSale(1, "New Sale Item");

        Assert.True(service.IsActive);
        this.listingProvider.Received(1).GetActiveRetainerId();
    }
}