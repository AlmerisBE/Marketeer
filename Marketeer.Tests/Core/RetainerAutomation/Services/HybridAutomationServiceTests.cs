using Dalamud.Plugin.Services;
using Marketeer.API.GameInterop.Contracts;
using Marketeer.Core.Configuration.Contracts;
using Marketeer.Core.Logging.Contracts;
using Marketeer.Core.MarketListings.Contracts;
using Marketeer.Core.MarketPricing.Contracts;
using Marketeer.Core.RetainerAutomation.Contracts;
using Marketeer.Core.RetainerAutomation.Services;
using Marketeer.Core.SalesHistory.Contracts;
using Marketeer.UI.Localization.Contracts;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Core.RetainerAutomation.Services;

public class HybridAutomationServiceTests {
    [Fact]
    public void HandleContextMenu_WhenOptionNotFound_ShouldAbortToPreventUiLock() {
        var framework = Substitute.For<IFramework>();
        var uiInteraction = Substitute.For<IRetainerUiInteractionService>();
        var priceProvider = Substitute.For<IMarketPriceCacheService>();
        var itemResolver = Substitute.For<IItemResolverService>();
        var configService = Substitute.For<IConfigurationService>();
        var objectTable = Substitute.For<IObjectTable>();
        var localization = Substitute.For<ILocalizationService>();
        var addonLifecycle = Substitute.For<IAddonLifecycle>();
        var logger = Substitute.For<ILoggerService>();
        var listingProvider = Substitute.For<IMarketListingProvider>();
        var listingTracker = Substitute.For<IMarketListingTrackerService>();
        var guidanceService = Substitute.For<IRetainerGuidanceService>();
        var keyState = Substitute.For<IKeyState>();

        uiInteraction.GetContextMenuItemIndex(Arg.Any<string>()).Returns(-1); // Simulate not found

        using var service = new HybridAutomationService(
            framework, uiInteraction, priceProvider, itemResolver, configService, objectTable,
            localization, addonLifecycle, logger, listingProvider, listingTracker, guidanceService, keyState);

        // Manually arm the service
        service.TriggerAdjustment();

        // Directly invoke the internal evaluation method, bypassing brittle Dalamud event mocking
        service.EvaluateContextMenuSetup();

        // The service MUST abort and deactivate instead of blindly clicking index 0
        Assert.False(service.IsActive);
        uiInteraction.DidNotReceiveWithAnyArgs().SelectContextMenuItem(default);
    }
}