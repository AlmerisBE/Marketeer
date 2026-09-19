using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Plugin.Services;
using Marketeer.API.GameInterop.Services;
using Marketeer.Core.Logging.Contracts;
using Marketeer.Core.MarketPricing.Contracts;
using Marketeer.Core.SalesHistory.Contracts;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Core.GameInterop.Services;

public class LocalMarketViewScannerTests {
    [Fact]
    public void Enable_ShouldRegisterLifecycleEvent() {
        var lifecycle = Substitute.For<IAddonLifecycle>();
        var priceProvider = Substitute.For<IMarketPriceCacheService>();
        var itemResolver = Substitute.For<IItemResolverService>();
        var objectTable = Substitute.For<IObjectTable>();
        var logger = Substitute.For<ILoggerService>();

        using var scanner = new LocalMarketViewScanner(lifecycle, priceProvider, itemResolver, objectTable, logger);

        scanner.Enable();

        lifecycle.Received(1).RegisterListener(AddonEvent.PostUpdate, "ItemSearchResult", Arg.Any<IAddonLifecycle.AddonEventDelegate>());
    }

    [Fact]
    public void Disable_ShouldUnregisterLifecycleEvent() {
        var lifecycle = Substitute.For<IAddonLifecycle>();
        var priceProvider = Substitute.For<IMarketPriceCacheService>();
        var itemResolver = Substitute.For<IItemResolverService>();
        var objectTable = Substitute.For<IObjectTable>();
        var logger = Substitute.For<ILoggerService>();

        using var scanner = new LocalMarketViewScanner(lifecycle, priceProvider, itemResolver, objectTable, logger);

        scanner.Enable();
        scanner.Disable();

        lifecycle.Received(1).UnregisterListener(AddonEvent.PostUpdate, "ItemSearchResult", Arg.Any<IAddonLifecycle.AddonEventDelegate>());
    }
}