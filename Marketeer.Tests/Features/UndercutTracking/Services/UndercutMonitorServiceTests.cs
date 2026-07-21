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
    public async Task CheckUndercutsAsync_WhenPricesAreLowerOnServer_UpdatesCompetitionStateAndPrintsToChat() {
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

        // Fully qualified System.Action resolves CS0104 ambiguity with Lumina.Excel.Sheets.Action
        mockFramework.RunOnFrameworkThread(Arg.Any<System.Action>())
            .Returns(Task.CompletedTask)
            .AndDoes(cb => cb.Arg<System.Action>()());

        mockClientState.IsLoggedIn.Returns(true);
        var mockPlayer = Substitute.For<IPlayerCharacter>();
        mockPlayer.CurrentWorld.Returns(new RowRef<World>(null, 33u));
        mockObjectTable.LocalPlayer.Returns(mockPlayer);

        var mySales = new List<RetainerListing> {
            new RetainerListing { ItemId = 100, RetainerName = "RetainerA", CurrentPrice = 5000 },
            new RetainerListing { ItemId = 200, RetainerName = "RetainerB", CurrentPrice = 3000 }
        };
        mockRetainerState.GetCurrentListings().Returns(mySales);

        var serverPrices = new List<LowestPriceResult> {
            new LowestPriceResult { ItemId = 100, Price = 4500, RetainerName = "CompetitorX" },
            new LowestPriceResult { ItemId = 200, Price = 3000, RetainerName = "RetainerB" }
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
            list.Count() == 1 && list.First().ItemId == 100
        ));

        mockChatGui.Received(1).Print("1 item undercut!");
    }

    [Fact]
    public async Task CheckUndercutsAsync_WhenNoUndercutsFound_DoesNotPrintToChat() {
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

        // Fully qualified System.Action resolves CS0104 ambiguity with Lumina.Excel.Sheets.Action
        mockFramework.RunOnFrameworkThread(Arg.Any<System.Action>())
            .Returns(Task.CompletedTask)
            .AndDoes(cb => cb.Arg<System.Action>()());

        mockClientState.IsLoggedIn.Returns(true);
        var mockPlayer = Substitute.For<IPlayerCharacter>();
        mockPlayer.CurrentWorld.Returns(new RowRef<World>(null, 33u));
        mockObjectTable.LocalPlayer.Returns(mockPlayer);

        var mySales = new List<RetainerListing> {
            new RetainerListing { ItemId = 100, RetainerName = "RetainerA", CurrentPrice = 3000 }
        };
        mockRetainerState.GetCurrentListings().Returns(mySales);

        var serverPrices = new List<LowestPriceResult> {
            new LowestPriceResult { ItemId = 100, Price = 3000, RetainerName = "RetainerA" }
        };
        mockPriceProvider.GetLowestPricesAsync(Arg.Any<IEnumerable<uint>>(), Arg.Any<uint>())
            .Returns(Task.FromResult<IReadOnlyList<LowestPriceResult>>(serverPrices));

        var service = new UndercutMonitorService(
            mockRetainerState, mockPriceProvider, mockCompetitionState, mockResolver,
            mockClientState, mockObjectTable, mockChatGui, mockLocalization, mockLogger, mockFramework);

        // Act
        await service.CheckUndercutsAsync();

        // Assert
        mockCompetitionState.Received(1).UpdateUndercuts(Arg.Is<IEnumerable<UndercutItem>>(list => !list.Any()));
        mockChatGui.DidNotReceiveWithAnyArgs().Print(default!);
    }
}