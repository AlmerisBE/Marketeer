using Dalamud.Plugin.Services;
using Marketeer.Core.SalesHistory.Contracts;
using Marketeer.UI.SalesHistory.Commands;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.UI.SalesHistory.Commands;

public class HistoryCommandTests {
    [Fact]
    public void Execute_WithResetArgument_ClearsSalesAndPrintsConfirmation() {
        // Arrange
        var mockRepository = Substitute.For<ISalesRepository>();
        var mockChatGui = Substitute.For<IChatGui>();
        var command = new HistoryCommand(mockRepository, mockChatGui);

        // Act
        command.Execute("clear");

        // Assert
        mockRepository.Received(1).ClearSales();
        mockChatGui.Received(1).Print("[Marketeer] Sales history has been successfully cleared.");
    }

    [Fact]
    public void Execute_WithInvalidOrEmptyArgument_PrintsErrorAndDoesNotClearSales() {
        // Arrange
        var mockRepository = Substitute.For<ISalesRepository>();
        var mockChatGui = Substitute.For<IChatGui>();
        var command = new HistoryCommand(mockRepository, mockChatGui);

        // Act
        command.Execute("unknown");

        // Assert
        mockRepository.DidNotReceive().ClearSales();
        mockChatGui.Received(1).PrintError("[Marketeer] Invalid argument. Usage: /marketeer history clear");
    }
}