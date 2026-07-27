using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin.Services;
using Marketeer.API.Configuration.Contracts;
using Marketeer.API.Configuration.Models;
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
    public void TriggerCancellation_StartsOrchestration_WhenSuboptimalListingsExist() {
        var mockOrchestrator = Substitute.For<IRetainerOrchestratorService>();
        var mockUiInteraction = Substitute.For<IRetainerUiInteractionService>();
        var mockOptimization = Substitute.For<IListingOptimizationService>();
        var mockInventory = Substitute.For<IInventoryService>();
        var mockObjectTable = Substitute.For<IObjectTable>();
        var mockLogger = Substitute.For<ILoggerService>();
        var mockConfig = Substitute.For<IConfigurationService>();
        var mockLocalization = Substitute.For<ILocalizationService>();

        mockConfig.GetConfig().Returns(new PluginConfiguration());

        var mockPlayer = Substitute.For<IPlayerCharacter>();
        mockPlayer.Name.Returns(new Dalamud.Game.Text.SeStringHandling.SeString(new Dalamud.Game.Text.SeStringHandling.Payloads.TextPayload("Test Player")));
        mockObjectTable.LocalPlayer.Returns(mockPlayer);

        mockOptimization.GetVendorPricedListings().Returns(new List<SuboptimalListing> {
            new SuboptimalListing { CharacterName = "Test Player", RetainerName = "Retainer1", ItemName = "Item1" }
        });

        var service = new CancelListingsAutomationService(
            mockOrchestrator,
            mockUiInteraction,
            mockOptimization,
            mockInventory,
            mockObjectTable,
            mockLogger,
            mockConfig,
            mockLocalization
        );

        service.TriggerCancellation();

        mockOrchestrator.Received(1).StartOrchestration(
            Arg.Is<IEnumerable<string>>(r => r.Contains("Retainer1")),
            RetainerTargetMenu.MarketListings,
            service
        );
    }
}