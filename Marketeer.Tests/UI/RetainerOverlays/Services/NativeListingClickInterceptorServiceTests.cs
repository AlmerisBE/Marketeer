using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.Gui.ContextMenu;
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
using System.Reflection;
using Xunit;

namespace Marketeer.Tests.UI.RetainerOverlays.Services;

public class NativeListingClickInterceptorServiceTests {

    [Fact]
    public void InterceptSequence_FromInventory_ShouldRouteToNewSale() {
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
        keyState[VirtualKey.SHIFT].Returns(true);

        gameGui.HoveredItem.Returns(500uL);
        itemResolver.ResolveItemName(500u).Returns("Test Inventory Item");

        // Mock UI translation keywords for ContextMenu parsing
        localization.Translate("InventoryMenu_SellOnMarket").Returns("Sell on Market Board");
        uiInteraction.GetContextMenuItemIndex(Arg.Any<IEnumerable<string>>()).Returns(2);

        using var service = new NativeListingClickInterceptorService(
            contextMenu, actionResolver, listingProvider, uiInteraction, itemResolver, localization, gameGui, keyState,
            configService, logger, hybridAutomation, cancellationService, framework);

        var menuArgs = Substitute.For<IMenuOpenedArgs>();
        menuArgs.AddonName.Returns("Inventory");

        // Safely trigger the private event handler via reflection to bypass specific Dalamud delegate requirements
        var method = typeof(NativeListingClickInterceptorService).GetMethod("OnMenuOpened", BindingFlags.NonPublic | BindingFlags.Instance);
        if (method != null) method.Invoke(service, new object[] { menuArgs });

        // Simulate framework tick progressing the state machine when ContextMenu UI is visible
        uiInteraction.IsAddonReady("ContextMenu").Returns(true);
        service.EvaluateTick();

        // Assert that the native context menu click was sent and the automation started
        uiInteraction.Received(1).SelectContextMenuItem(2);
        hybridAutomation.Received(1).StartNewSale(500u, "Test Inventory Item");
        actionResolver.DidNotReceive().ProcessListingClick(Arg.Any<TrackedListing>());
    }

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
        keyState[VirtualKey.SHIFT].Returns(true);

        gameGui.HoveredItem.Returns(200uL);
        itemResolver.ResolveItemName(200u).Returns("Test Dragged Item");

        // Mock the new O(1) data-binding resolution mechanism
        itemResolver.ResolveItemId("Test Dragged Item ").Returns(200u);

        using var service = new NativeListingClickInterceptorService(
            contextMenu, actionResolver, listingProvider, uiInteraction, itemResolver, localization, gameGui, keyState,
            configService, logger, hybridAutomation, cancellationService, framework);

        uiInteraction.IsAddonReady("RetainerSell").Returns(false);
        service.EvaluateTick();

        uiInteraction.IsAddonReady("RetainerSell").Returns(true);

        var texts = new List<string> { "Test Dragged Item " };
        uiInteraction.GetActiveRetainerSellItemData(out Arg.Any<List<string>>(), out Arg.Any<uint>())
            .Returns(x => {
                x[0] = texts;
                x[1] = 0u;
                return true;
            });

        service.EvaluateTick();

        hybridAutomation.Received(1).StartNewSale(200u, "Test Dragged Item ");
        actionResolver.DidNotReceive().ProcessListingClick(Arg.Any<TrackedListing>());
    }
}