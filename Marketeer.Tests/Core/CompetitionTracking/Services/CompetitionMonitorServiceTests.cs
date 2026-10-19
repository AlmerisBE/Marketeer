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
    public async Task CheckUndercutsAsync_ValidatesListingsAndMutatesState() {
        var retainerState = Substitute.For<IRetainerStateService>();
        var priceProvider = Substitute.For<IMarketPriceCacheService>();
        var stateMutator = Substitute.For<ICompetitionStateMutator>();
        var evaluator = Substitute.For<ICompetitionEvaluatorService>();
        var itemResolver = Substitute.For<IItemResolverService>();
        var listingTracker = Substitute.For<IMarketListingTrackerService>();
        var priceCalc = Substitute.For<IPriceCalculationService>();
        var chatGui = Substitute.For<IChatGui>();
        var localization = Substitute.For<ILocalizationService>();
        var logger = Substitute.For<ILoggerService>();
        var configService = Substitute.For<IConfigurationService>();
        var clientState = Substitute.For<IClientState>();
        var framework = Substitute.For<IFramework>();

        configService.GetConfig().Returns(new PluginConfiguration { EnableChatNotifications = false });

        var charData = new CharacterMarketData {
            CharacterName = "TestPlayer",
            HomeWorldId = 33u,
            Listings = new List<RetainerListing> {
                new RetainerListing { ItemId = 1, CurrentPrice = 5000 }
            }
        };

        retainerState.GetAllCharactersListings().Returns(new List<CharacterMarketData> { charData });

        var pricing = new MarketItemPricing { ItemId = 1 };
        priceProvider.GetPricingsAsync(Arg.Any<IEnumerable<uint>>(), Arg.Any<uint>(), false)
            .Returns(Task.FromResult<IReadOnlyList<MarketItemPricing>>(new List<MarketItemPricing> { pricing }));

        itemResolver.ResolveItemName(1).Returns("Test Item");

        var mockUndercut = new UndercutItem { ItemId = 1, ItemName = "Test Item" };

        evaluator.TryEvaluateListing(
            Arg.Any<RetainerListing>(),
            Arg.Any<MarketItemPricing>(),
            Arg.Any<string>(),
            Arg.Any<uint>(),
            Arg.Any<string>(),
            out Arg.Any<UndercutItem?>()
        ).Returns(x => {
            x[5] = mockUndercut;
            return true;
        });

        var service = new CompetitionMonitorService(
            retainerState, priceProvider, stateMutator, evaluator, itemResolver,
            listingTracker, priceCalc, chatGui, localization, logger, configService, clientState, framework);

        await service.CheckUndercutsAsync();

        stateMutator.Received(1).UpdateUndercuts(Arg.Is<IEnumerable<UndercutItem>>(list => list.Contains(mockUndercut)));
    }

    [Fact]
    public async Task CheckUndercutsAsync_WhenSameUndercutsPersist_DoesNotSpamChat() {
        var retainerState = Substitute.For<IRetainerStateService>();
        var priceProvider = Substitute.For<IMarketPriceCacheService>();
        var stateMutator = Substitute.For<ICompetitionStateMutator>();
        var evaluator = Substitute.For<ICompetitionEvaluatorService>();
        var itemResolver = Substitute.For<IItemResolverService>();
        var listingTracker = Substitute.For<IMarketListingTrackerService>();
        var priceCalc = Substitute.For<IPriceCalculationService>();
        var chatGui = Substitute.For<IChatGui>();
        var localization = Substitute.For<ILocalizationService>();
        var logger = Substitute.For<ILoggerService>();
        var configService = Substitute.For<IConfigurationService>();
        var clientState = Substitute.For<IClientState>();
        var framework = Substitute.For<IFramework>();

        // Ensure notifications are explicitly enabled for this specific test
        configService.GetConfig().Returns(new PluginConfiguration { EnableChatNotifications = true });

        var charData = new CharacterMarketData {
            CharacterName = "TestPlayer",
            HomeWorldId = 33u,
            Listings = new List<RetainerListing> {
                new RetainerListing { ItemId = 1, CurrentPrice = 5000 }
            }
        };

        retainerState.GetAllCharactersListings().Returns(new List<CharacterMarketData> { charData });

        var pricing = new MarketItemPricing { ItemId = 1 };
        priceProvider.GetPricingsAsync(Arg.Any<IEnumerable<uint>>(), Arg.Any<uint>(), false)
            .Returns(Task.FromResult<IReadOnlyList<MarketItemPricing>>(new List<MarketItemPricing> { pricing }));

        itemResolver.ResolveItemName(1).Returns("Test Item");

        var mockUndercut = new UndercutItem { ItemId = 1, ItemName = "Test Item" };

        evaluator.TryEvaluateListing(
            Arg.Any<RetainerListing>(),
            Arg.Any<MarketItemPricing>(),
            Arg.Any<string>(),
            Arg.Any<uint>(),
            Arg.Any<string>(),
            out Arg.Any<UndercutItem?>()
        ).Returns(x => {
            x[5] = mockUndercut;
            return true;
        });

        localization.Translate(Arg.Any<string>(), Arg.Any<object[]>()).Returns("[Marketeer] Items undercut!");

        var service = new CompetitionMonitorService(
            retainerState, priceProvider, stateMutator, evaluator, itemResolver,
            listingTracker, priceCalc, chatGui, localization, logger, configService, clientState, framework);

        // First pass: a new undercut is detected, so it MUST print to the chat.
        await service.CheckUndercutsAsync();
        chatGui.Received(1).Print(Arg.Any<string>());

        // Second pass: the exact same undercut is detected, it must NOT print again to prevent spam.
        await service.CheckUndercutsAsync();
        chatGui.Received(1).Print(Arg.Any<string>()); // Assertion remains at 1
    }
}