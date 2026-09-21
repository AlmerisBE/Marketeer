using Dalamud.Plugin.Services;
using Marketeer.API.GameInterop.Contracts;
using Marketeer.Core.CompetitionTracking.Contracts;
using Marketeer.Core.MarketListings.Contracts;
using Marketeer.Core.RetainerAutomation.Contracts;
using Marketeer.UI.Localization.Contracts;
using Marketeer.UI.RetainerOverlays.Commands;
using Marketeer.UI.RetainerOverlays.Contracts;
using Marketeer.UI.RetainerOverlays.UI;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.UI.Guidance.Commands;

public class GuidanceCommandTests {
    [Fact]
    public void Execute_ShouldToggleWindowIsOpenState() {
        var geometryProvider = Substitute.For<IWindowGeometryProvider>();
        var instructionProviders = new List<IGuidanceInstructionProvider>();
        var localization = Substitute.For<ILocalizationService>();
        var switcherService = Substitute.For<IRetainerSwitcherService>();
        var competitionState = Substitute.For<ICompetitionStateService>();
        var optimizationService = Substitute.For<IListingOptimizationService>();
        var objectTable = Substitute.For<IObjectTable>();
        var listingProvider = Substitute.For<IMarketListingProvider>();
        var retainerProvider = Substitute.For<IRetainerProvider>();
        var uiInteraction = Substitute.For<IRetainerUiInteractionService>();

        // Create the actual window instance with mocked dependencies
        var window = new MarketeerGuideWindow(
            geometryProvider, instructionProviders, localization, switcherService,
            competitionState, optimizationService, objectTable, listingProvider,
            retainerProvider, uiInteraction);

        var command = new GuidanceCommand(window, localization);

        // Ensure the initial state is closed for testing
        window.IsOpen = false;

        // First execution should open it
        command.Execute("");
        Assert.True(window.IsOpen);

        // Second execution should close it
        command.Execute("");
        Assert.False(window.IsOpen);
    }
}