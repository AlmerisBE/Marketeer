using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin.Services;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using Marketeer.Features.Localization.Contracts;
using Marketeer.Features.Logging.Contracts;
using Marketeer.Features.SalesHistoryTracking.Contracts;
using Marketeer.Features.UndercutTracking.Commands;
using Marketeer.Features.UndercutTracking.Contracts;
using Marketeer.Features.UndercutTracking.Models;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Features.UndercutTracking.Commands;

public class PriceCommandTests {
    [Fact]
    public void Execute_WithInvalidArguments_PrintsError() {
        // Arrange
        var mockProvider = Substitute.For<IServerPriceProvider>();
        var mockObjectTable = Substitute.For<IObjectTable>();
        var mockResolver = Substitute.For<IItemResolverService>();
        var mockChatGui = Substitute.For<IChatGui>();
        var mockLocalization = Substitute.For<ILocalizationService>();
        var mockLogger = Substitute.For<ILoggerService>();

        mockLocalization.Translate("Command_Price_InvalidArgs").Returns("Invalid args");

        var command = new PriceCommand(mockProvider, mockObjectTable, mockResolver, mockChatGui, mockLocalization, mockLogger);

        // Act
        command.Execute("highest Potion");

        // Assert
        mockChatGui.Received(1).PrintError("Invalid args");
        mockResolver.DidNotReceiveWithAnyArgs().ResolveItemId(default!);
    }

    [Fact]
    public void Execute_WithUnresolvedItem_PrintsItemNotFoundError() {
        // Arrange
        var mockProvider = Substitute.For<IServerPriceProvider>();
        var mockObjectTable = Substitute.For<IObjectTable>();
        var mockResolver = Substitute.For<IItemResolverService>();
        var mockChatGui = Substitute.For<IChatGui>();
        var mockLocalization = Substitute.For<ILocalizationService>();
        var mockLogger = Substitute.For<ILoggerService>();

        mockResolver.ResolveItemId("FakeItem").Returns(0u);
        mockLocalization.Translate("Command_Price_ItemNotFound", "FakeItem").Returns("Not found");

        var command = new PriceCommand(mockProvider, mockObjectTable, mockResolver, mockChatGui, mockLocalization, mockLogger);

        // Act
        command.Execute("lowest FakeItem");

        // Assert
        mockChatGui.Received(1).PrintError("Not found");
        mockProvider.DidNotReceiveWithAnyArgs().GetLowestPriceAsync(default, default);
    }

    [Fact]
    public async Task Execute_WithValidItem_FetchesAndPrintsPrice() {
        // Arrange
        var mockProvider = Substitute.For<IServerPriceProvider>();
        var mockObjectTable = Substitute.For<IObjectTable>();
        var mockResolver = Substitute.For<IItemResolverService>();
        var mockChatGui = Substitute.For<IChatGui>();
        var mockLocalization = Substitute.For<ILocalizationService>();
        var mockLogger = Substitute.For<ILoggerService>();

        var mockPlayer = Substitute.For<IPlayerCharacter>();

        // Fix CS1503: CurrentWorld returns a RowRef<World> wrapper in Lumina V4 / API 14+
        mockPlayer.CurrentWorld.Returns(default(RowRef<World>));
        mockObjectTable.LocalPlayer.Returns(mockPlayer);

        mockResolver.ResolveItemId("Potion").Returns(42u);
        mockResolver.ResolveItemName(42u).Returns("Potion");

        var expectedResult = new LowestPriceResult { ItemId = 42u, Price = 100u, RetainerName = "Almeris" };

        // Ensure we test against the default RowId of 0u since we passed default(RowRef<World>)
        mockProvider.GetLowestPriceAsync(42u, 0u).Returns(Task.FromResult<LowestPriceResult?>(expectedResult));

        mockLocalization.Translate("Command_Price_Fetching", "Potion").Returns("Fetching...");
        mockLocalization.Translate("Command_Price_Result", "Potion", "100", "Almeris").Returns("Lowest price for Potion: 100 Gil (Retainer: Almeris)");

        var command = new PriceCommand(mockProvider, mockObjectTable, mockResolver, mockChatGui, mockLocalization, mockLogger);

        // Act
        command.Execute("lowest Potion");

        // Allow task to execute on thread pool
        await Task.Delay(100);

        // Assert
        mockChatGui.Received(1).Print("Fetching...");
        mockChatGui.Received(1).Print("Lowest price for Potion: 100 Gil (Retainer: Almeris)");
    }
}