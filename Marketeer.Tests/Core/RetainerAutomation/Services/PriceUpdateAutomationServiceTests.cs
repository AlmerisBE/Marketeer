using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin.Services;
using Marketeer.API.GameInterop.Contracts;
using Marketeer.Core.CompetitionTracking.Contracts;
using Marketeer.Core.CompetitionTracking.Models;
using Marketeer.Core.Configuration.Contracts;
using Marketeer.Core.Logging.Contracts;
using Marketeer.Core.RetainerAutomation.Contracts;
using Marketeer.Core.RetainerAutomation.Models;
using Marketeer.Core.RetainerAutomation.Services;
using Marketeer.UI.Localization.Contracts;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Core.RetainerAutomation.Services;

public class PriceUpdateAutomationServiceTests {
    [Fact]
    public void TriggerSingleItemUpdate_WithValidItem_ShouldStartOrchestrationForSingleRetainer() {
        // Arrange
        var mockOrchestrator = Substitute.For<IRetainerOrchestratorService>();
        var mockUiInteraction = Substitute.For<IRetainerUiInteractionService>();
        var mockCompetitionState = Substitute.For<ICompetitionStateService>();
        var mockInventory = Substitute.For<IInventoryService>();
        var mockObjectTable = Substitute.For<IObjectTable>();
        var mockLogger = Substitute.For<ILoggerService>();
        var mockConfig = Substitute.For<IConfigurationService>();
        var mockLocalization = Substitute.For<ILocalizationService>();

        var mockPlayer = Substitute.For<IPlayerCharacter>();
        mockPlayer.Name.Returns(new Dalamud.Game.Text.SeStringHandling.SeString(
            new List<Dalamud.Game.Text.SeStringHandling.Payload> {
                new Dalamud.Game.Text.SeStringHandling.Payloads.TextPayload("Test Player")
            }
        ));
        mockObjectTable.LocalPlayer.Returns(mockPlayer);

        var undercuts = new List<UndercutItem> {
            new UndercutItem { ItemId = 100, CharacterName = "Test Player", RetainerName = "Retainer A" },
            new UndercutItem { ItemId = 200, CharacterName = "Test Player", RetainerName = "Retainer B" }
        };
        mockCompetitionState.GetUndercutItems().Returns(undercuts);

        var service = new PriceUpdateAutomationService(
            mockOrchestrator, mockUiInteraction, mockCompetitionState, mockInventory,
            mockObjectTable, mockLogger, mockConfig, mockLocalization);

        // Act
        service.TriggerSingleItemUpdate(200);

        // Assert
        mockOrchestrator.Received(1).StartOrchestration(
            Arg.Is<IEnumerable<string>>(list => list.Count() == 1 && list.First() == "Retainer B"),
            RetainerTargetMenu.MarketListings,
            service
        );
    }
}