using Dalamud.Game.ClientState.Keys;
using Dalamud.Plugin.Services;
using Marketeer.API.GameInterop.Contracts;
using Marketeer.Core.Configuration.Contracts;
using Marketeer.Core.Configuration.Models;
using Marketeer.Core.Logging.Contracts;
using Marketeer.Core.MarketListings.Models;
using Marketeer.Core.RetainerAutomation.Contracts;
using Marketeer.Core.RetainerAutomation.Models;
using Marketeer.Core.SalesHistory.Contracts;
using Marketeer.UI.Localization.Contracts;
using Marketeer.UI.RetainerOverlays.Services;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.UI.RetainerOverlays.Services;

public class NativeListingClickInterceptorServiceTests {
    [Fact]
    public void EvaluateRetainerSell_WhenResolverReturnsCancel_ShouldTriggerCancellation() {
        var addonLifecycle = Substitute.For<IAddonLifecycle>();
        var hybridAutomation = Substitute.For<IHybridAutomationService>();
        var cancellationService = Substitute.For<IListingCancellationService>();
        var actionResolver = Substitute.For<IListingActionResolverService>();
        var uiInteraction = Substitute.For<IRetainerUiInteractionService>();
        var listingProvider = Substitute.For<IMarketListingProvider>();
        var itemResolver = Substitute.For<IItemResolverService>();
        var keyState = Substitute.For<IKeyState>();
        var configService = Substitute.For<IConfigurationService>();
        var localization = Substitute.For<ILocalizationService>();
        var logger = Substitute.For<ILoggerService>();
        var framework = Substitute.For<IFramework>();

        var config = new PluginConfiguration { AutoSellModifierKey = ModifierKey.Shift };
        configService.GetConfig().Returns(config);
        keyState[VirtualKey.SHIFT].Returns(true);

        var texts = new List<string> { "Test Item" };
        uiInteraction.GetActiveRetainerSellItemData(out Arg.Any<List<string>>(), out Arg.Any<uint>())
            .Returns(x => {
                x[0] = texts;
                x[1] = 500u;
                return true;
            });

        itemResolver.ResolveItemId("Test Item").Returns(100u);
        actionResolver.ResolveAction(100u).Returns(ListingClickAction.CancelListing);

        using var service = new NativeListingClickInterceptorService(
            addonLifecycle, hybridAutomation, cancellationService, actionResolver, uiInteraction,
            listingProvider, itemResolver, keyState, configService, localization, logger, framework);

        service.EvaluateRetainerSell(123456);

        cancellationService.Received(1).TriggerCancellation(100u);
        uiInteraction.Received(1).CloseUnexpectedWindows();
        hybridAutomation.DidNotReceive().StartPriceUpdate(Arg.Any<TrackedListing>(), Arg.Any<nint>());
    }
}