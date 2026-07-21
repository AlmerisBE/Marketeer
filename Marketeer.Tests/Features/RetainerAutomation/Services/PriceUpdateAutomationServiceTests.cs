using Dalamud.Plugin.Services;
using Marketeer.Features.Inventory.Contracts;
using Marketeer.Features.Logging.Contracts;
using Marketeer.Features.RetainerAutomation.Contracts;
using Marketeer.Features.RetainerAutomation.Services;
using Marketeer.Features.RetainerOrchestration.Contracts;
using Marketeer.Features.RetainerOrchestration.Models;
using Marketeer.Features.UndercutTracking.Contracts;
using Marketeer.Features.UndercutTracking.Models;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Features.RetainerAutomation.Services;

public class PriceUpdateAutomationServiceTests {

    [Fact]
    public void TriggerPriceUpdate_WithUndercuts_StartsOrchestration() {
        // Arrange
        var mockOrchestrator = Substitute.For<IRetainerOrchestratorService>();
        var mockUiInteraction = Substitute.For<IRetainerUiInteractionService>();
        var mockCompetitionState = Substitute.For<ICompetitionStateService>();
        var mockInventoryService = Substitute.For<IInventoryService>();
        var mockObjectTable = Substitute.For<IObjectTable>();
        var mockLogger = Substitute.For<ILoggerService>();

        var mockPlayer = Substitute.For<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>();
        mockPlayer.Name.Returns(new Dalamud.Game.Text.SeStringHandling.SeString(new Dalamud.Game.Text.SeStringHandling.Payloads.TextPayload("Tester")));
        mockObjectTable.LocalPlayer.Returns(mockPlayer);

        mockCompetitionState.GetUndercutItems().Returns(new List<UndercutItem> {
            new UndercutItem { CharacterName = "Tester", RetainerName = "RetainerA", SlotIndex = 0, ServerCheapestPrice = 500 }
        });

        var service = new PriceUpdateAutomationService(
            mockOrchestrator, mockUiInteraction, mockCompetitionState, mockInventoryService, mockObjectTable, mockLogger);

        // Act
        service.TriggerPriceUpdate();

        // Assert
        mockOrchestrator.Received(1).StartOrchestration(
            Arg.Is<IEnumerable<string>>(r => r.Contains("RetainerA")),
            RetainerTargetMenu.MarketListings,
            service);
    }
}