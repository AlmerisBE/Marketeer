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
    public async Task CheckUndercutForItemAsync_UpdatesSpecificItemState() {
        var retainerState = Substitute.For<IRetainerStateService>();
        var priceProvider = Substitute.For<IServerPriceProvider>();
        var competitionState = Substitute.For<ICompetitionStateService>();
        var itemResolver = Substitute.For<IItemResolverService>();
        var configService = Substitute.For<IConfigurationService>();
        var chatGui = Substitute.For<IChatGui>();
        var localization = Substitute.For<ILocalizationService>();
        var logger = Substitute.For<ILoggerService>();
        var marketListingTracker = Substitute.For<IMarketListingTrackerService>();
        var framework = Substitute.For<IFramework>();

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

        // Typage explicite en IReadOnlyList pour correspondre parfaitement au contrat de l'interface
        IReadOnlyList<LowestPriceResult> marketPrices = new List<LowestPriceResult> {
            new LowestPriceResult { ItemId = 100, Price = 4000, RetainerName = "CompetitorX", IsHq = false }
        };

        // Encapsulation explicite dans Task.FromResult pour assurer la résolution correcte du Mock
        priceProvider.GetLowestPricesAsync(Arg.Any<IEnumerable<uint>>(), Arg.Any<uint>())
            .Returns(Task.FromResult(marketPrices));

        var service = new CompetitionMonitorService(
            retainerState, priceProvider, competitionState, itemResolver, marketListingTracker,
            chatGui, localization, logger, framework, configService);

        await service.CheckUndercutForItemAsync(100);

        // Utilisation de .Any() pour éviter les erreurs d'évaluation de NSubstitute sur les listes vides
        competitionState.Received(1).UpdateItemUndercuts(
            100,
            Arg.Is<IEnumerable<UndercutItem>>(list => list.Any(u => u.CompetitorName == "CompetitorX"))
        );
    }

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
        var framework = Substitute.For<IFramework>();

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

        priceProvider.GetLowestPricesAsync(Arg.Any<IEnumerable<uint>>(), Arg.Any<uint>())
            .Returns(Task.FromResult(marketPrices));

        var service = new CompetitionMonitorService(
            retainerState, priceProvider, competitionState, itemResolver, marketListingTracker,
            chatGui, localization, logger, framework, configService);

        await service.CheckUndercutsAsync();

        competitionState.Received(1).UpdateUndercuts(
            Arg.Is<IEnumerable<UndercutItem>>(list => list.Any(u => u.CompetitorName == "CompetitorX"))
        );
    }
}