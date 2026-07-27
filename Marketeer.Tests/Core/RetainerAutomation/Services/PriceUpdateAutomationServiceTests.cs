using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin.Services;
using Marketeer.API.CompetitionTracking.Contracts;
using Marketeer.API.CompetitionTracking.Models;
using Marketeer.API.Configuration.Contracts;
using Marketeer.API.Configuration.Models;
using Marketeer.API.GameInterop.Contracts;
using Marketeer.API.Localization.Contracts;
using Marketeer.API.Logging.Contracts;
using Marketeer.API.RetainerAutomation.Contracts;
using Marketeer.API.RetainerAutomation.Models;
using Marketeer.Core.RetainerAutomation.Services;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Core.RetainerAutomation.Services;

public class PriceUpdateAutomationServiceTests {
    [Fact]
    public void TriggerPriceUpdate_StartsOrchestration_WhenUndercutsExist() {
        var mockOrchestrator = Substitute.For<IRetainerOrchestratorService>();
        var mockCompetition = Substitute.For<ICompetitionStateService>();
        var mockObjectTable = Substitute.For<IObjectTable>();
        var mockConfig = Substitute.For<IConfigurationService>();

        mockConfig.GetConfig().Returns(new PluginConfiguration());

        var mockPlayer = Substitute.For<IPlayerCharacter>();
        mockPlayer.Name.Returns(new Dalamud.Game.Text.SeStringHandling.SeString(new Dalamud.Game.Text.SeStringHandling.Payloads.TextPayload("Test Player")));
        mockObjectTable.LocalPlayer.Returns(mockPlayer);

        mockCompetition.GetUndercutItems().Returns(new List<UndercutItem> {
            new UndercutItem { CharacterName = "Test Player", RetainerName = "Retainer1", ItemName = "Item1" }
        });

        var service = new PriceUpdateAutomationService(
            mockOrchestrator,
            Substitute.For<IRetainerUiInteractionService>(),
            mockCompetition,
            Substitute.For<IInventoryService>(),
            mockObjectTable,
            Substitute.For<ILoggerService>(),
            mockConfig,
            Substitute.For<ILocalizationService>()
        );

        service.TriggerPriceUpdate();

        mockOrchestrator.Received(1).StartOrchestration(
            Arg.Is<IEnumerable<string>>(r => r.Contains("Retainer1")),
            RetainerTargetMenu.MarketListings,
            service
        );
    }
}