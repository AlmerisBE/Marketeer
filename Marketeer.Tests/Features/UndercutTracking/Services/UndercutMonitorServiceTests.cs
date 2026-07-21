using Dalamud.Plugin.Services;
using Marketeer.Features.Dashboard.Contracts;
using Marketeer.Features.Localization.Contracts;
using Marketeer.Features.Logging.Contracts;
using Marketeer.Features.SalesHistoryTracking.Contracts;
using Marketeer.Features.UndercutTracking.Contracts;
using Marketeer.Features.UndercutTracking.Models;
using Marketeer.Features.UndercutTracking.Services;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Features.UndercutTracking.Services;

public class UndercutMonitorServiceTests {

    [Fact]
    public async Task CheckUndercutsAsync_WhenPricesAreLowerOnServer_UpdatesCompetitionStateWithCompetitorNameAndCharacter() {
        // Arrange
        var mockRetainerState = Substitute.For<IRetainerStateService>();
        var mockPriceProvider = Substitute.For<IServerPriceProvider>();
        var mockCompetitionState = Substitute.For<ICompetitionStateService>();
        var mockResolver = Substitute.For<IItemResolverService>();
        var mockChatGui = Substitute.For<IChatGui>();
        var mockLocalization = Substitute.For<ILocalizationService>();
        var mockLogger = Substitute.For<ILoggerService>();
        var mockFramework = Substitute.For<IFramework>();
        var mockMarketTracker = Substitute.For<IMarketListingTrackerService>();

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

        var service = new UndercutMonitorService(
            mockRetainerState, mockPriceProvider, mockCompetitionState, mockResolver,
            mockMarketTracker, mockChatGui, mockLocalization, mockLogger, mockFramework);

        // Act
        await service.CheckUndercutsAsync();

        // Assert
        mockCompetitionState.Received(1).UpdateUndercuts(Arg.Is<IEnumerable<UndercutItem>>(list =>
            list.Count() == 1 &&
            list.First().ItemId == 100 &&
            list.First().CompetitorName == "CompetitorX" &&
            list.First().CharacterName == "TestPlayer"
        ));
    }

    // Ajoute ce test à la suite existante
    [Fact]
    public async Task CheckUndercutForItemAsync_UpdatesSpecificItemState() {
        // Arrange
        var mockRetainerState = Substitute.For<IRetainerStateService>();
        var mockPriceProvider = Substitute.For<IServerPriceProvider>();
        var mockCompetitionState = Substitute.For<ICompetitionStateService>();
        var mockResolver = Substitute.For<IItemResolverService>();
        var mockMarketTracker = Substitute.For<IMarketListingTrackerService>();
        var mockChatGui = Substitute.For<IChatGui>();
        var mockLocalization = Substitute.For<ILocalizationService>();
        var mockLogger = Substitute.For<ILoggerService>();
        var mockFramework = Substitute.For<IFramework>();

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

        var service = new UndercutMonitorService(
            mockRetainerState, mockPriceProvider, mockCompetitionState, mockResolver,
            mockMarketTracker, mockChatGui, mockLocalization, mockLogger, mockFramework);

        // Act
        await service.CheckUndercutForItemAsync(100);

        // Assert
        mockCompetitionState.Received(1).UpdateItemUndercuts(100, Arg.Is<IEnumerable<UndercutItem>>(list =>
            list.Count() == 1 && list.First().CompetitorName == "CompetitorX"
        ));
    }
}