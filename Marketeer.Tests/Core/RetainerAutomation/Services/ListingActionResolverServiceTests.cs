using Marketeer.API.GameInterop.Contracts;
using Marketeer.Core.Configuration.Contracts;
using Marketeer.Core.Configuration.Models;
using Marketeer.Core.Logging.Contracts;
using Marketeer.Core.MarketListings.Models;
using Marketeer.Core.RetainerAutomation.Contracts;
using Marketeer.Core.RetainerAutomation.Models;
using Marketeer.Core.RetainerAutomation.Services;
using Marketeer.Core.SalesHistory.Contracts;
using Marketeer.UI.CompetitionTracking.Contracts;
using Marketeer.UI.Localization.Contracts;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Core.RetainerAutomation.Services;

public class ListingActionResolverServiceTests {
    [Fact]
    public void ResolveAction_WhenLossConditionMet_ReturnsCancelListing() {
        var configService = Substitute.For<IConfigurationService>();
        var listingProvider = Substitute.For<IMarketListingProvider>();
        var competitionState = Substitute.For<ICompetitionStateService>();
        var itemResolver = Substitute.For<IItemResolverService>();
        var logger = Substitute.For<ILoggerService>();
        var hybridAutomation = Substitute.For<IHybridAutomationService>();
        var cancellationService = Substitute.For<IListingCancellationService>();
        var notificationService = Substitute.For<INotificationService>();
        var localization = Substitute.For<ILocalizationService>();

        var config = new PluginConfiguration { EnforceVendorPriceMinimum = true, LossBehavior = MinimumPriceBehavior.CancelToInventory };
        configService.GetConfig().Returns(config);

        itemResolver.ResolveVendorPrice(100u).Returns(500u);
        listingProvider.GetActiveRetainerListings().Returns(new List<TrackedListing> {
            new TrackedListing { ItemId = 100u, PricePerUnit = 400u }
        });

        using var service = new ListingActionResolverService(
            configService, listingProvider, competitionState, itemResolver, logger,
            hybridAutomation, cancellationService, notificationService, localization);

        var result = service.ResolveAction(100u);

        Assert.Equal(ListingClickAction.CancelListing, result);
    }

    [Fact]
    public void ProcessListingClick_ShouldRouteToUpdatePriceByDefault() {
        var configService = Substitute.For<IConfigurationService>();
        var listingProvider = Substitute.For<IMarketListingProvider>();
        var competitionState = Substitute.For<ICompetitionStateService>();
        var itemResolver = Substitute.For<IItemResolverService>();
        var logger = Substitute.For<ILoggerService>();
        var hybridAutomation = Substitute.For<IHybridAutomationService>();
        var cancellationService = Substitute.For<IListingCancellationService>();
        var notificationService = Substitute.For<INotificationService>();
        var localization = Substitute.For<ILocalizationService>();

        var config = new PluginConfiguration { EnforceVendorPriceMinimum = false };
        configService.GetConfig().Returns(config);

        using var service = new ListingActionResolverService(
            configService, listingProvider, competitionState, itemResolver, logger,
            hybridAutomation, cancellationService, notificationService, localization);

        var listing = new TrackedListing { ItemId = 200u, ItemName = "Test" };

        service.ProcessListingClick(listing);

        hybridAutomation.Received(1).StartPriceUpdate(listing);
        cancellationService.DidNotReceive().TriggerCancellation(Arg.Any<uint>());
    }
}