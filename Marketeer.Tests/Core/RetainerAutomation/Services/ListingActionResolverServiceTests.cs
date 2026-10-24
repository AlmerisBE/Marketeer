using Marketeer.API.GameInterop.Contracts;
using Marketeer.Core.Configuration.Contracts;
using Marketeer.Core.Configuration.Models;
using Marketeer.Core.Logging.Contracts;
using Marketeer.Core.MarketListings.Models;
using Marketeer.Core.MarketPricing.Models;
using Marketeer.Core.MarketStrategy.Models;
using Marketeer.Core.RetainerAutomation.Contracts;
using Marketeer.Core.RetainerAutomation.Models;
using Marketeer.Core.RetainerAutomation.Services;
using Marketeer.Core.SalesHistory.Contracts;
using Marketeer.UI.CompetitionTracking.Contracts;
using Marketeer.UI.CompetitionTracking.Models;
using Marketeer.UI.Localization.Contracts;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Core.RetainerAutomation.Services;

public class ListingActionResolverServiceTests {
    private IConfigurationService configService;
    private IMarketListingProvider listingProvider;
    private ICompetitionStateService competitionState;
    private IItemResolverService itemResolver;
    private ILoggerService logger;
    private IHybridAutomationService hybridAutomation;
    private IListingCancellationService cancellationService;
    private INotificationService notificationService;
    private ILocalizationService localization;

    public ListingActionResolverServiceTests() {
        this.configService = Substitute.For<IConfigurationService>();
        this.listingProvider = Substitute.For<IMarketListingProvider>();
        this.competitionState = Substitute.For<ICompetitionStateService>();
        this.itemResolver = Substitute.For<IItemResolverService>();
        this.logger = Substitute.For<ILoggerService>();
        this.hybridAutomation = Substitute.For<IHybridAutomationService>();
        this.cancellationService = Substitute.For<IListingCancellationService>();
        this.notificationService = Substitute.For<INotificationService>();
        this.localization = Substitute.For<ILocalizationService>();

        this.configService.GetConfig().Returns(new PluginConfiguration());
        this.competitionState.GetUndercutItems().Returns(new List<UndercutItem>());
    }

    private ListingActionResolverService CreateService() {
        return new ListingActionResolverService(
            this.configService,
            this.listingProvider,
            this.competitionState,
            this.itemResolver,
            this.logger,
            this.hybridAutomation,
            this.cancellationService,
            this.notificationService,
            this.localization
        );
    }

    [Fact]
    public void ResolveAction_WithNullListing_ReturnsNone() {
        var service = this.CreateService();
        var result = service.ResolveAction(null!);

        Assert.Equal(ListingClickAction.None, result);
    }

    [Fact]
    public void ResolveAction_WithStorageProtectionTriggered_ReturnsNone() {
        this.configService.GetConfig().Returns(new PluginConfiguration {
            EnableStorageProtection = true,
            StorageProtectionThreshold = 9999999
        });

        var service = this.CreateService();
        var listing = new TrackedListing { ItemId = 1, PricePerUnit = 10000000 }; // Price above threshold

        var result = service.ResolveAction(listing);

        Assert.Equal(ListingClickAction.None, result);
    }

    [Fact]
    public void ResolveAction_WithMarketCrashAnomaly_ReturnsNone() {
        var config = new PluginConfiguration { AnomalyStrategy = AnomalyDefenseStrategy.HoldPrice };
        this.configService.GetConfig().Returns(config);

        var anomalyUndercut = new UndercutItem {
            ItemId = 1,
            AnomalyData = new AnomalyReport { IsAnomalyDetected = true }
        };
        this.competitionState.GetUndercutItems().Returns(new List<UndercutItem> { anomalyUndercut });

        var service = this.CreateService();
        var listing = new TrackedListing { ItemId = 1, PricePerUnit = 5000 };

        var result = service.ResolveAction(listing);

        Assert.Equal(ListingClickAction.None, result);
    }

    [Fact]
    public void ResolveAction_WithVendorRedLineCrossed_ReturnsCancelListing() {
        var config = new PluginConfiguration {
            EnforceVendorPriceMinimum = true,
            LossBehavior = MinimumPriceBehavior.CancelToInventory
        };
        this.configService.GetConfig().Returns(config);

        this.itemResolver.ResolveVendorPrice(1).Returns(1000u); // Vendor pays 1000g

        var service = this.CreateService();
        var listing = new TrackedListing { ItemId = 1, PricePerUnit = 900u }; // We are selling for 900g

        var result = service.ResolveAction(listing);

        Assert.Equal(ListingClickAction.CancelListing, result);
    }

    [Fact]
    public void ResolveAction_WithNormalUndercut_ReturnsUpdatePrice() {
        var config = new PluginConfiguration { EnforceVendorPriceMinimum = true };
        this.configService.GetConfig().Returns(config);

        this.itemResolver.ResolveVendorPrice(1).Returns(100u);
        this.competitionState.GetUndercutItems().Returns(new List<UndercutItem> {
            new UndercutItem { ItemId = 1, SuggestedAction = PricingAction.DecreasePrice }
        });

        var service = this.CreateService();
        var listing = new TrackedListing { ItemId = 1, PricePerUnit = 5000u };

        var result = service.ResolveAction(listing);

        Assert.Equal(ListingClickAction.UpdatePrice, result);
    }
}