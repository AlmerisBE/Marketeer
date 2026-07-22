using Dalamud.Plugin.Services;
using Marketeer.API.Configuration.Contracts;
using Marketeer.API.GameInterop.Contracts;
using Marketeer.API.Localization.Contracts;
using Marketeer.API.Logging.Contracts;
using Marketeer.API.MarketListings.Contracts;
using Marketeer.API.MarketListings.Models;
using Marketeer.API.RetainerAutomation.Contracts;
using Marketeer.API.RetainerAutomation.Models;
using Marketeer.Core.RetainerAutomation.Services;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Core.RetainerAutomation.Services;

public class CancelListingsAutomationServiceTests {
    [Fact]
    public void TriggerCancellation_WhenNoSuboptimalListings_DoesNotStartOrchestration() {
        // Arrange
        var mockOrchestrator = Substitute.For<IRetainerOrchestratorService>();
        var mockUiInteraction = Substitute.For<IRetainerUiInteractionService>();
        var mockOptimization = Substitute.For<IListingOptimizationService>();
        var mockInventory = Substitute.For<IInventoryService>();
        var mockObjectTable = Substitute.For<IObjectTable>();
        var mockLogger = Substitute.For<ILoggerService>();
        var mockConfig = Substitute.For<IConfigurationService>();
        var mockLocalization = Substitute.For<ILocalizationService>();

        var mockPlayer = Substitute.For<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>();
        mockPlayer.Name.Returns(new Dalamud.Game.Text.SeStringHandling.SeString(
            new List<Dalamud.Game.Text.SeStringHandling.Payload> {
                new Dalamud.Game.Text.SeStringHandling.Payloads.TextPayload("Test Player")
            }));
        mockObjectTable.LocalPlayer.Returns(mockPlayer);

        mockOptimization.GetVendorPricedListings().Returns(new List<SuboptimalListing>());

        var service = new CancelListingsAutomationService(
            mockOrchestrator, mockUiInteraction, mockOptimization,
            mockInventory, mockObjectTable, mockLogger, mockConfig, mockLocalization);

        // Act
        service.TriggerCancellation();

        // Assert
        mockOrchestrator.DidNotReceive().StartOrchestration(Arg.Any<IEnumerable<string>>(), Arg.Any<RetainerTargetMenu>(), Arg.Any<IRetainerTask>());
    }

    [Fact]
    public void TriggerCancellation_WhenSuboptimalListingsExist_StartsOrchestration() {
        // Arrange
        var mockOrchestrator = Substitute.For<IRetainerOrchestratorService>();
        var mockUiInteraction = Substitute.For<IRetainerUiInteractionService>();
        var mockOptimization = Substitute.For<IListingOptimizationService>();
        var mockInventory = Substitute.For<IInventoryService>();
        var mockObjectTable = Substitute.For<IObjectTable>();
        var mockLogger = Substitute.For<ILoggerService>();
        var mockConfig = Substitute.For<IConfigurationService>();
        var mockLocalization = Substitute.For<ILocalizationService>();

        var mockPlayer = Substitute.For<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>();
        mockPlayer.Name.Returns(new Dalamud.Game.Text.SeStringHandling.SeString(
            new List<Dalamud.Game.Text.SeStringHandling.Payload> {
                new Dalamud.Game.Text.SeStringHandling.Payloads.TextPayload("Test Player")
            }));
        mockObjectTable.LocalPlayer.Returns(mockPlayer);

        mockOptimization.GetVendorPricedListings().Returns(new List<SuboptimalListing> {
            new SuboptimalListing { CharacterName = "Test Player", RetainerName = "Retainer B" }
        });

        var service = new CancelListingsAutomationService(
            mockOrchestrator, mockUiInteraction, mockOptimization,
            mockInventory, mockObjectTable, mockLogger, mockConfig, mockLocalization);

        // Act
        service.TriggerCancellation();

        // Assert
        mockOrchestrator.Received(1).StartOrchestration(Arg.Any<IEnumerable<string>>(), Arg.Any<RetainerTargetMenu>(), Arg.Any<IRetainerTask>());
    }
}