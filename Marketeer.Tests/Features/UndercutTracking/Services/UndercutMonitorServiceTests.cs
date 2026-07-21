using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin.Services;
using Lumina.Excel;
using Lumina.Excel.Sheets;
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
    public async Task CheckUndercutsAsync_WhenPricesAreLowerOnServer_UpdatesCompetitionStateWithCompetitorName() {
        // Arrange
        var mockRetainerState = Substitute.For<IRetainerStateService>();
        var mockPriceProvider = Substitute.For<IServerPriceProvider>();
        var mockCompetitionState = Substitute.For<ICompetitionStateService>();
        var mockResolver = Substitute.For<IItemResolverService>();
        var mockClientState = Substitute.For<IClientState>();
        var mockObjectTable = Substitute.For<IObjectTable>();
        var mockChatGui = Substitute.For<IChatGui>();
        var mockLocalization = Substitute.For<ILocalizationService>();
        var mockLogger = Substitute.For<ILoggerService>();
        var mockFramework = Substitute.For<IFramework>();

        mockFramework.RunOnFrameworkThread(Arg.Any<System.Action>())
            .Returns(Task.CompletedTask)
            .AndDoes(cb => cb.Arg<System.Action>()());

        mockClientState.IsLoggedIn.Returns(true);
        var mockPlayer = Substitute.For<IPlayerCharacter>();
        mockPlayer.CurrentWorld.Returns(new RowRef<World>(null, 33u));
        mockObjectTable.LocalPlayer.Returns(mockPlayer);

        var mySales = new List<RetainerListing> {
            new RetainerListing { ItemId = 100, RetainerName = "RetainerA", CurrentPrice = 5000 }
        };
        mockRetainerState.GetCurrentListings().Returns(mySales);

        var serverPrices = new List<LowestPriceResult> {
            new LowestPriceResult { ItemId = 100, Price = 4500, RetainerName = "CompetitorX" }
        };
        mockPriceProvider.GetLowestPricesAsync(Arg.Any<IEnumerable<uint>>(), Arg.Any<uint>())
            .Returns(Task.FromResult<IReadOnlyList<LowestPriceResult>>(serverPrices));

        mockResolver.ResolveItemName(100).Returns("Potion");
        mockLocalization.Translate("Undercuts_Notification", 1).Returns("1 item undercut!");

        var service = new UndercutMonitorService(
            mockRetainerState, mockPriceProvider, mockCompetitionState, mockResolver,
            mockClientState, mockObjectTable, mockChatGui, mockLocalization, mockLogger, mockFramework);

        // Act
        await service.CheckUndercutsAsync();

        // Assert
        mockCompetitionState.Received(1).UpdateUndercuts(Arg.Is<IEnumerable<UndercutItem>>(list =>
            list.Count() == 1 &&
            list.First().ItemId == 100 &&
            list.First().CompetitorName == "CompetitorX"
        ));
    }
}