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
    public async Task CheckUndercutsAsync_WhenPricesAreLowerOnServer_UpdatesCompetitionStateWithCompetitorNameAndCharacter() {
        var mockRetainerState = Substitute.For<IRetainerStateService>();
        var mockPriceProvider = Substitute.For<IServerPriceProvider>();
        var mockCompetitionState = Substitute.For<ICompetitionStateService>();
        var mockResolver = Substitute.For<IItemResolverService>();
        var mockChatGui = Substitute.For<IChatGui>();
        var mockLocalization = Substitute.For<ILocalizationService>();
        var mockLogger = Substitute.For<ILoggerService>();
        var mockFramework = Substitute.For<IFramework>();
        var mockMarketTracker = Substitute.For<IMarketListingTrackerService>();
        var mockConfigService = Substitute.For<IConfigurationService>();

        mockConfigService.GetConfig().Returns(new PluginConfiguration());

        var characterData = new List<CharacterMarketData> {
            new CharacterMarketData {
                CharacterName = "TestPlayer",
                HomeWorldId = 33u,
                Listings = new List<RetainerListing> {
                    new RetainerListing { ItemId = 100, RetainerName = "RetainerA", CurrentPrice = 5000 }
                }
            }
        };

        mockRetainerState.GetAllCharactersListings().Returns(characterData);

        var serverPrices = new List<LowestPriceResult> {
            new LowestPriceResult { ItemId = 100, Price = 4500, RetainerName = "CompetitorX" }
        };
        mockPriceProvider.GetLowestPricesAsync(Arg.Any<IEnumerable<uint>>(), 33u)
            .Returns(Task.FromResult<IReadOnlyList<LowestPriceResult>>(serverPrices));

        mockResolver.ResolveItemName(100).Returns("Potion");
        mockLocalization.Translate("Undercuts_Notification", 1).Returns("1 item undercut!");

        var service = new CompetitionMonitorService(
            mockRetainerState, mockPriceProvider, mockCompetitionState, mockResolver,
            mockMarketTracker, mockChatGui, mockLocalization, mockLogger, mockFramework, mockConfigService);

        await service.CheckUndercutsAsync();

        mockCompetitionState.Received(1).UpdateUndercuts(Arg.Is<IEnumerable<UndercutItem>>(list =>
            list.Count() == 1 &&
            list.First().ItemId == 100 &&
            list.First().CompetitorName == "CompetitorX" &&
            list.First().CharacterName == "TestPlayer"
        ));
    }

    [Fact]
    public async Task CheckUndercutForItemAsync_UpdatesSpecificItemState() {
        var mockRetainerState = Substitute.For<IRetainerStateService>();
        var mockPriceProvider = Substitute.For<IServerPriceProvider>();
        var mockCompetitionState = Substitute.For<ICompetitionStateService>();
        var mockResolver = Substitute.For<IItemResolverService>();
        var mockMarketTracker = Substitute.For<IMarketListingTrackerService>();
        var mockChatGui = Substitute.For<IChatGui>();
        var mockLocalization = Substitute.For<ILocalizationService>();
        var mockLogger = Substitute.For<ILoggerService>();
        var mockFramework = Substitute.For<IFramework>();
        var mockConfigService = Substitute.For<IConfigurationService>();

        mockConfigService.GetConfig().Returns(new PluginConfiguration());

        var characterData = new List<CharacterMarketData> {
            new CharacterMarketData {
                CharacterName = "TestPlayer",
                HomeWorldId = 33u,
                Listings = new List<RetainerListing> {
                    new RetainerListing { ItemId = 100, RetainerName = "RetainerA", CurrentPrice = 5000 }
                }
            }
        };

        mockRetainerState.GetAllCharactersListings().Returns(characterData);
        mockPriceProvider.GetLowestPriceAsync(100, 33u).Returns(Task.FromResult<LowestPriceResult?>(new LowestPriceResult { Price = 4000, RetainerName = "CompetitorX" }));
        mockResolver.ResolveItemName(100).Returns("Potion");

        var service = new CompetitionMonitorService(
            mockRetainerState, mockPriceProvider, mockCompetitionState, mockResolver,
            mockMarketTracker, mockChatGui, mockLocalization, mockLogger, mockFramework, mockConfigService);

        await service.CheckUndercutForItemAsync(100);

        mockCompetitionState.Received(1).UpdateItemUndercuts(100, Arg.Is<IEnumerable<UndercutItem>>(list =>
            list.Count() == 1 && list.First().CompetitorName == "CompetitorX"
        ));
    }
}