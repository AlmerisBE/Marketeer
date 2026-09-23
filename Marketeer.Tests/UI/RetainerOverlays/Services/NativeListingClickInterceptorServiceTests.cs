using Dalamud.Game.ClientState.Keys;
using Dalamud.Plugin.Services;
using Marketeer.API.GameInterop.Contracts;
using Marketeer.Core.Configuration.Contracts;
using Marketeer.Core.Configuration.Models;
using Marketeer.Core.Logging.Contracts;
using Marketeer.Core.MarketListings.Models;
using Marketeer.Core.RetainerAutomation.Contracts;
using Marketeer.Core.SalesHistory.Contracts;
using Marketeer.UI.Localization.Contracts;
using Marketeer.UI.RetainerOverlays.Services;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.UI.RetainerOverlays.Services;

public class NativeListingClickInterceptorServiceTests {
    // ... Garde le test précédent "InterceptSequence_FromInventory_ShouldRouteToNewSale" ici ...

    [Fact]
    public void EvaluateTick_WhenRetainerSellOpensViaDragAndDropWithModifier_ShouldValidateFastPathAndRouteToNewSale() {
        var contextMenu = Substitute.For<IContextMenu>();
        var actionResolver = Substitute.For<IListingActionResolverService>();
        var listingProvider = Substitute.For<IMarketListingProvider>();
        var uiInteraction = Substitute.For<IRetainerUiInteractionService>();
        var itemResolver = Substitute.For<IItemResolverService>();
        var localization = Substitute.For<ILocalizationService>();
        var gameGui = Substitute.For<IGameGui>();
        var keyState = Substitute.For<IKeyState>();
        var configService = Substitute.For<IConfigurationService>();
        var logger = Substitute.For<ILoggerService>();
        var hybridAutomation = Substitute.For<IHybridAutomationService>();
        var cancellationService = Substitute.For<IListingCancellationService>();
        var framework = Substitute.For<IFramework>();

        var config = new PluginConfiguration { AutoSellModifierKey = ModifierKey.Shift };
        configService.GetConfig().Returns(config);
        keyState[VirtualKey.SHIFT].Returns(true); // Modifier is held

        // Pre-load the memory cache for the dragged item
        gameGui.HoveredItem.Returns(200uL);
        itemResolver.ResolveItemName(200u).Returns("Test Dragged Item");

        using var service = new NativeListingClickInterceptorService(
            contextMenu, actionResolver, listingProvider, uiInteraction, itemResolver, localization, gameGui, keyState,
            configService, logger, hybridAutomation, cancellationService, framework);

        // Frame 1: RetainerSell is not visible (wasRetainerSellVisible becomes false)
        uiInteraction.IsAddonReady("RetainerSell").Returns(false);
        service.EvaluateTick();

        // Frame 2: User performs a drag and drop, RetainerSell opens directly
        uiInteraction.IsAddonReady("RetainerSell").Returns(true);

        // Fast-path resolution check
        var texts = new List<string> { "Test Dragged Item " }; // Simulating High Quality marker from native UI
        uiInteraction.GetActiveRetainerSellItemData(out Arg.Any<List<string>>(), out Arg.Any<uint>())
            .Returns(x => {
                x[0] = texts;
                x[1] = 0u;
                return true;
            });

        service.EvaluateTick();

        // Assert - Hybrid Automation handles the active sale directly from the fast-path resolution
        hybridAutomation.Received(1).StartNewSale(200u, "Test Dragged Item");
        actionResolver.DidNotReceive().ProcessListingClick(Arg.Any<TrackedListing>());
    }
}