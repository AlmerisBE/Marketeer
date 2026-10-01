using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Plugin.Services;
using Marketeer.API.GameInterop.Contracts;
using Marketeer.Core.Logging.Contracts;
using Marketeer.Core.MarketListings.Contracts;
using Marketeer.Core.SalesHistory.Contracts;
using Marketeer.UI.CompetitionTracking.Contracts;
using Marketeer.UI.RetainerOverlays.Services;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Core.MarketListings.Services;

public class NativeListingHighlighterServiceTests {

    private class TestableNativeListingHighlighterService : NativeListingHighlighterService {
        public TestableNativeListingHighlighterService(
            IAddonLifecycle addonLifecycle,
            IGameGui gameGui,
            IMarketListingProvider listingProvider,
            IRetainerProvider retainerProvider,
            ICompetitionStateService competitionState,
            IListingOptimizationService optimizationService,
            IInventoryService inventoryService,
            IItemResolverService itemResolver,
            IObjectTable objectTable,
            ILoggerService logger) : base(
                addonLifecycle, gameGui, listingProvider, retainerProvider, competitionState, optimizationService, inventoryService, itemResolver, objectTable, logger) { }

        public string? ExposeGetMatchingItemName(string uiText, HashSet<string> allItems) {
            return this.GetMatchingItemName(uiText, allItems);
        }
    }

    [Fact]
    public void Service_RegistersAndUnregistersLifecycleEvents() {
        var addonLifecycle = Substitute.For<IAddonLifecycle>();
        var service = new TestableNativeListingHighlighterService(
            addonLifecycle,
            Substitute.For<IGameGui>(),
            Substitute.For<IMarketListingProvider>(),
            Substitute.For<IRetainerProvider>(),
            Substitute.For<ICompetitionStateService>(),
            Substitute.For<IListingOptimizationService>(),
            Substitute.For<IInventoryService>(),
            Substitute.For<IItemResolverService>(),
            Substitute.For<IObjectTable>(),
            Substitute.For<ILoggerService>()
        );

        addonLifecycle.Received(1).RegisterListener(AddonEvent.PostUpdate, "RetainerSellList", Arg.Any<IAddonLifecycle.AddonEventDelegate>());

        service.Dispose();

        addonLifecycle.Received(1).UnregisterListener(AddonEvent.PostUpdate, "RetainerSellList", Arg.Any<IAddonLifecycle.AddonEventDelegate>());
    }

    [Fact]
    public void GetMatchingItemName_MatchesExactString_ReturnsItemName() {
        var service = new TestableNativeListingHighlighterService(
            Substitute.For<IAddonLifecycle>(), Substitute.For<IGameGui>(), Substitute.For<IMarketListingProvider>(), Substitute.For<IRetainerProvider>(),
            Substitute.For<ICompetitionStateService>(), Substitute.For<IListingOptimizationService>(), Substitute.For<IInventoryService>(),
            Substitute.For<IItemResolverService>(), Substitute.For<IObjectTable>(), Substitute.For<ILoggerService>()
        );

        var items = new HashSet<string> { "Apple", "Banana", "Cherry" };
        var result = service.ExposeGetMatchingItemName("Banana", items);

        Assert.Equal("Banana", result);
    }

    [Fact]
    public void GetMatchingItemName_MatchesTruncatedString_ReturnsItemName() {
        var service = new TestableNativeListingHighlighterService(
            Substitute.For<IAddonLifecycle>(), Substitute.For<IGameGui>(), Substitute.For<IMarketListingProvider>(), Substitute.For<IRetainerProvider>(),
            Substitute.For<ICompetitionStateService>(), Substitute.For<IListingOptimizationService>(), Substitute.For<IInventoryService>(),
            Substitute.For<IItemResolverService>(), Substitute.For<IObjectTable>(), Substitute.For<ILoggerService>()
        );

        var items = new HashSet<string> { "Super Long Item Name That Gets Cut Off", "Apple" };
        var result = service.ExposeGetMatchingItemName("Super Long Item Name That Ge...", items);

        Assert.Equal("Super Long Item Name That Gets Cut Off", result);
    }

    [Fact]
    public void GetMatchingItemName_StripsHqSymbol_ReturnsItemName() {
        var service = new TestableNativeListingHighlighterService(
            Substitute.For<IAddonLifecycle>(), Substitute.For<IGameGui>(), Substitute.For<IMarketListingProvider>(), Substitute.For<IRetainerProvider>(),
            Substitute.For<ICompetitionStateService>(), Substitute.For<IListingOptimizationService>(), Substitute.For<IInventoryService>(),
            Substitute.For<IItemResolverService>(), Substitute.For<IObjectTable>(), Substitute.For<ILoggerService>()
        );

        var items = new HashSet<string> { "High Quality Sword" };
        var result = service.ExposeGetMatchingItemName("\uE03CHigh Quality Sword", items);

        Assert.Equal("High Quality Sword", result);
    }
}