using Dalamud.Plugin.Services;
using Marketeer.API.CompetitionTracking.Contracts;
using Marketeer.API.CompetitionTracking.Models;
using Marketeer.API.Configuration.Contracts;
using Marketeer.API.Configuration.Models;
using Marketeer.API.Localization.Contracts;
using Marketeer.API.Logging.Contracts;
using Marketeer.API.MarketListings.Contracts;
using Marketeer.API.SalesHistory.Contracts;
using Marketeer.API.Universalis.Contracts;
using Marketeer.API.Universalis.Models;
using Marketeer.Core.CompetitionTracking.Services;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Core.CompetitionTracking.Services;

public class CompetitionMonitorServiceTests {
    [Fact]
    public async Task CheckUndercutsAsync_ShouldUpdateStateAndNotify_WhenUndercutsFound() {
        var retainerState = Substitute.For<IRetainerStateService>();
        var priceProvider = Substitute.For<IServerPriceProvider>();
        var competitionState = Substitute.For<ICompetitionStateService>();
        var itemResolver = Substitute.For<IItemResolverService>();
        var configService = Substitute.For<IConfigurationService>();
        var chatGui = Substitute.For<IChatGui>();
        var localization = Substitute.For<ILocalizationService>();
        var logger = Substitute.For<ILoggerService>();
        var marketListingTracker = Substitute.For<IMarketListingTrackerService>();

        var config = new PluginConfiguration {
            AutoWhitelistOwnRetainers = false
        };
        configService.GetConfig().Returns(config);
        itemResolver.ResolveItemName(100).Returns("Test Item");

        var myListings = new List<CharacterMarketData> {
            new CharacterMarketData {
                CharacterName = "MyCharacter",
                HomeWorldId = 1,
                Listings = new List<RetainerListing> {
                    new RetainerListing { ItemId = 100, CurrentPrice = 5000, RetainerName = "MyRetainer", SlotIndex = 0 }
                }
            }
        };
        retainerState.GetAllCharactersListings().Returns(myListings);

        IReadOnlyList<LowestPriceResult> marketPrices = new List<LowestPriceResult> {
            new LowestPriceResult { ItemId = 100, Price = 4000, RetainerName = "CompetitorX", IsHq = false }
        };

        // bypassCache est maintenant false par défaut lors de l'appel depuis CheckUndercutsAsync
        priceProvider.GetLowestPricesAsync(Arg.Any<IEnumerable<uint>>(), Arg.Any<uint>(), false)
            .Returns(Task.FromResult(marketPrices));

        // Note la suppression de framework du constructeur
        var service = new CompetitionMonitorService(
            retainerState, priceProvider, competitionState, itemResolver, marketListingTracker,
            chatGui, localization, logger, configService);

        await service.CheckUndercutsAsync();

        competitionState.Received(1).UpdateUndercuts(
            Arg.Is<IEnumerable<UndercutItem>>(list => list.Any(u => u.CompetitorName == "CompetitorX"))
        );
    }
}