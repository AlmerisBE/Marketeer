using Dalamud.Plugin.Services;
using Marketeer.API.CompetitionTracking.Contracts;
using Marketeer.API.CompetitionTracking.Models;
using Marketeer.API.Configuration.Contracts;
using Marketeer.API.GameInterop.Contracts;
using Marketeer.API.Logging.Contracts;
using Marketeer.API.RetainerAutomation.Contracts;
using Marketeer.API.RetainerAutomation.Models;
using Marketeer.Core.RetainerAutomation.Services;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Core.RetainerAutomation.Services;

public class PriceUpdateAutomationServiceTests {
    [Fact]
    public void TriggerPriceUpdate_WithUndercuts_StartsOrchestration() {
        var mockOrchestrator = Substitute.For<IRetainerOrchestratorService>();
        var mockUiInteraction = Substitute.For<IRetainerUiInteractionService>();
        var mockCompetitionState = Substitute.For<ICompetitionStateService>();
        var mockInventoryService = Substitute.For<IInventoryService>();
        var mockObjectTable = Substitute.For<IObjectTable>();
        var mockLogger = Substitute.For<ILoggerService>();
        var mockConfigService = Substitute.For<IConfigurationService>();

        var mockPlayer = Substitute.For<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>();
        mockPlayer.Name.Returns(new Dalamud.Game.Text.SeStringHandling.SeString(new Dalamud.Game.Text.SeStringHandling.Payloads.TextPayload("Tester")));
        mockObjectTable.LocalPlayer.Returns(mockPlayer);

        mockCompetitionState.GetUndercutItems().Returns(new List<UndercutItem> {
            new UndercutItem { CharacterName = "Tester", RetainerName = "RetainerA", SlotIndex = 0, ServerCheapestPrice = 500 }
        });

        var service = new PriceUpdateAutomationService(
            mockOrchestrator, mockUiInteraction, mockCompetitionState, mockInventoryService, mockObjectTable, mockLogger, mockConfigService);

        service.TriggerPriceUpdate();

        mockOrchestrator.Received(1).StartOrchestration(
            Arg.Is<IEnumerable<string>>(r => r.Contains("RetainerA")),
            RetainerTargetMenu.MarketListings,
            service);
    }
}