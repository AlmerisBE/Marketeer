using Dalamud.Plugin.Services;
using Marketeer.API.Universalis.Models;
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
    public async Task CheckUndercutsAsync_WhenUndercutDetected_UpdatesState() {
        var retainerState = Substitute.For<IRetainerStateService>();
        var priceCache = Substitute.For<IMarketPriceCacheService>();
        var compState = Substitute.For<ICompetitionStateMutator>();
        var resolver = Substitute.For<IItemResolverService>();
        var tracker = Substitute.For<IMarketListingTrackerService>();
        var calcService = Substitute.For<IPriceCalculationService>();
        var chat = Substitute.For<IChatGui>();
        var loc = Substitute.For<ILocalizationService>();
        var logger = Substitute.For<ILoggerService>();
        var config = Substitute.For<IConfigurationService>();
        var clientState = Substitute.For<IClientState>();
        var framework = Substitute.For<IFramework>();

        config.GetConfig().Returns(new PluginConfiguration { EnableChatNotifications = false });

        var listings = new List<RetainerListing> {
            new RetainerListing { ItemId = 123, CurrentPrice = 2000, RetainerName = "MyRetainer" }
        };
        var charData = new CharacterMarketData { CharacterName = "TestChar", HomeWorldId = 73, Listings = listings };

        retainerState.GetAllCharactersListings().Returns(new List<CharacterMarketData> { charData });

        var pricing = new MarketItemPricing {
            ItemId = 123,
            Listings = new List<LowestPriceResult> {
                new LowestPriceResult { Price = 1500, RetainerName = "Competitor" }
            }
        };

        priceCache.GetPricingsAsync(Arg.Any<IEnumerable<uint>>(), 73u, false)
                  .Returns(new List<MarketItemPricing> { pricing });

        calcService.CalculateTargetPrice(123u, 2000u, Arg.Any<MarketItemPricing>())
                   .Returns(new PriceCalculationResult { Action = PricingAction.UpdatePrice, CalculatedPrice = 1499 });

        var service = new CompetitionMonitorService(
            retainerState, priceCache, compState, resolver, tracker, calcService,
            chat, loc, logger, config, clientState, framework);

        await service.CheckUndercutsAsync();

        compState.Received(1).UpdateUndercuts(Arg.Is<IEnumerable<UndercutItem>>(u => u.GetEnumerator().MoveNext()));
    }
}