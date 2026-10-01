using Dalamud.Plugin.Services;
using Marketeer.Core.CompetitionTracking.Contracts;
using Marketeer.Core.CompetitionTracking.Models;
using Marketeer.Core.CompetitionTracking.Services;
using Marketeer.Core.Configuration.Contracts;
using Marketeer.Core.Configuration.Models;
using Marketeer.Core.Logging.Contracts;
using Marketeer.Core.MarketListings.Contracts;
using Marketeer.Core.MarketPricing.Contracts;
using Marketeer.Core.MarketPricing.Models;
using Marketeer.Core.SalesHistory.Contracts;
using Marketeer.UI.CompetitionTracking.Models;
using Marketeer.UI.Localization.Contracts;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Core.CompetitionTracking.Services;

public class CompetitionMonitorServiceTests {
    [Fact]
    public async Task CheckUndercutsAsync_WhenSameUndercutsPersist_DoesNotSpamChat() {
        // Arrange
        var retainerState = Substitute.For<IRetainerStateService>();
        var priceProvider = Substitute.For<IMarketPriceCacheService>();
        var stateMutator = Substitute.For<ICompetitionStateMutator>();
        var evaluatorService = Substitute.For<ICompetitionEvaluatorService>();
        var itemResolver = Substitute.For<IItemResolverService>();
        var marketListingTracker = Substitute.For<IMarketListingTrackerService>();
        var priceCalcService = Substitute.For<IPriceCalculationService>();
        var chatGui = Substitute.For<IChatGui>();
        var localization = Substitute.For<ILocalizationService>();
        var logger = Substitute.For<ILoggerService>();
        var configService = Substitute.For<IConfigurationService>();
        var clientState = Substitute.For<IClientState>();
        var framework = Substitute.For<IFramework>();

        configService.GetConfig().Returns(new PluginConfiguration { EnableChatNotifications = true });
        localization.Translate("Undercuts_Notification", Arg.Any<int>()).Returns("Items undercut!");

        var service = new CompetitionMonitorService(
            retainerState, priceProvider, stateMutator, evaluatorService, itemResolver,
            marketListingTracker, priceCalcService, chatGui, localization, logger,
            configService, clientState, framework);

        // Mock state: 1 item listed, and it is undercut
        var charMarketData = new CharacterMarketData {
            CharacterName = "Player",
            HomeWorldId = 33,
            Listings = new List<RetainerListing> { new RetainerListing { ItemId = 123 } }
        };
        retainerState.GetAllCharactersListings().Returns(new List<CharacterMarketData> { charMarketData });

        var pricing = new List<MarketItemPricing> { new MarketItemPricing { ItemId = 123 } };
        priceProvider.GetPricingsAsync(Arg.Any<IEnumerable<uint>>(), 33, false).Returns(pricing);

        var undercutItem = new UndercutItem { ItemId = 123 };
        evaluatorService.TryEvaluateListing(Arg.Any<RetainerListing>(), Arg.Any<MarketItemPricing>(), Arg.Any<string>(), Arg.Any<string>(), out Arg.Any<UndercutItem?>())
            .Returns(x => {
                x[4] = undercutItem;
                return true;
            });

        // Act - Trigger multiple evaluations sequentially (simulating multiple API batches loading)
        await service.CheckUndercutsAsync();
        await service.CheckUndercutsAsync();
        await service.CheckUndercutsAsync();

        // Assert - The chat message should only trigger once, because the set of undercuts { 123 } never changed
        chatGui.Received(1).Print(Arg.Any<string>());
    }
}