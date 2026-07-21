using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin.Services;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using Marketeer.API.Localization.Contracts;
using Marketeer.API.Logging.Contracts;
using Marketeer.API.SalesHistory.Contracts;
using Marketeer.API.Universalis.Contracts;
using Marketeer.API.Universalis.Models;
using Marketeer.UI.CompetitionTracking.Commands;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.UI.CompetitionTracking.Commands;

public class PriceCommandTests {

    [Fact]
    public async Task Execute_WithPlainItemName_ResolvesItemIdAndFetchesPrice() {
        // Arrange
        var mockProvider = Substitute.For<IServerPriceProvider>();
        var mockObjectTable = Substitute.For<IObjectTable>();
        var mockResolver = Substitute.For<IItemResolverService>();
        var mockChatGui = Substitute.For<IChatGui>();
        var mockLocalization = Substitute.For<ILocalizationService>();
        var mockLogger = Substitute.For<ILoggerService>();

        var mockPlayer = Substitute.For<IPlayerCharacter>();
        mockPlayer.CurrentWorld.Returns(new RowRef<World>(null, 33u));
        mockObjectTable.LocalPlayer.Returns(mockPlayer);

        uint expectedItemId = 100u;
        uint priceValue = 1500u;
        string formattedPrice = priceValue.ToString("N0");
        string itemName = "Potion";
        string retainerName = "Crafter";

        mockResolver.ResolveItemId(itemName).Returns(expectedItemId);
        mockResolver.ResolveItemName(expectedItemId).Returns(itemName);

        mockLocalization.Translate("Command_Price_Fetching", itemName)
            .Returns("Fetching lowest price for Potion...");

        mockLocalization.Translate("Command_Price_Result", itemName, formattedPrice, retainerName)
            .Returns($"Lowest price for Potion: {formattedPrice} Gil (Retainer: Crafter)");

        var expectedResult = new LowestPriceResult { ItemId = expectedItemId, Price = priceValue, RetainerName = retainerName };
        mockProvider.GetLowestPriceAsync(expectedItemId, 33u).Returns(Task.FromResult<LowestPriceResult?>(expectedResult));

        var command = new PriceCommand(mockProvider, mockObjectTable, mockResolver, mockChatGui, mockLocalization, mockLogger);

        // Act
        command.Execute("lowest Potion");

        await Task.Delay(100);

        // Assert
        mockResolver.Received(1).ResolveItemId("Potion");
        mockResolver.Received(1).ResolveItemName(expectedItemId);

        mockChatGui.Received(2).Print(Arg.Any<string>());
        mockChatGui.Received(1).Print("Fetching lowest price for Potion...");
        mockChatGui.Received(1).Print($"Lowest price for Potion: {formattedPrice} Gil (Retainer: Crafter)");
    }
}