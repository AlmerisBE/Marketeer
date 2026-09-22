using Marketeer.API.GameInterop.Contracts;
using Marketeer.Core.CompetitionTracking.Contracts;
using Marketeer.Core.CompetitionTracking.Models;
using Marketeer.Core.Configuration.Contracts;
using Marketeer.Core.Configuration.Models;
using Marketeer.Core.Logging.Contracts;
using Marketeer.Core.MarketPricing.Models;
using Marketeer.Core.RetainerAutomation.Models;
using Marketeer.Core.RetainerAutomation.Services;
using Marketeer.Core.SalesHistory.Contracts;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Core.RetainerAutomation.Services;

public class ListingActionResolverServiceTests {
    [Fact]
    public void ResolveAction_WhenRedLineDetected_ShouldReturnCancelListing() {
        var configService = Substitute.For<IConfigurationService>();
        var listingProvider = Substitute.For<IMarketListingProvider>();
        var competitionState = Substitute.For<ICompetitionStateService>();
        var itemResolver = Substitute.For<IItemResolverService>();
        var logger = Substitute.For<ILoggerService>();

        configService.GetConfig().Returns(new PluginConfiguration { LossBehavior = MinimumPriceBehavior.CancelToInventory });

        competitionState.GetUndercutItems().Returns(new List<UndercutItem> {
            new UndercutItem { ItemId = 100, SuggestedAction = PricingAction.CancelListing }
        });

        var service = new ListingActionResolverService(configService, listingProvider, competitionState, itemResolver, logger);

        var result = service.ResolveAction(100);

        Assert.Equal(ListingClickAction.CancelListing, result);
    }

    [Fact]
    public void ResolveAction_WhenStandardItem_ShouldReturnUpdatePrice() {
        var configService = Substitute.For<IConfigurationService>();
        var listingProvider = Substitute.For<IMarketListingProvider>();
        var competitionState = Substitute.For<ICompetitionStateService>();
        var itemResolver = Substitute.For<IItemResolverService>();
        var logger = Substitute.For<ILoggerService>();

        configService.GetConfig().Returns(new PluginConfiguration { LossBehavior = MinimumPriceBehavior.CancelToInventory });
        competitionState.GetUndercutItems().Returns(new List<UndercutItem>());

        var service = new ListingActionResolverService(configService, listingProvider, competitionState, itemResolver, logger);

        var result = service.ResolveAction(100);

        Assert.Equal(ListingClickAction.UpdatePrice, result);
    }
}