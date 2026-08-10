using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Plugin.Services;
using Marketeer.API.CompetitionTracking.Contracts;
using Marketeer.API.Logging.Contracts;
using Marketeer.API.MarketListings.Contracts;
using Marketeer.Core.MarketListings.Services;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Core.MarketListings.Services;

public class NativeRetainerListHighlighterServiceTests {

    [Fact]
    public void Service_RegistersAndUnregistersLifecycleEvents() {
        var addonLifecycle = Substitute.For<IAddonLifecycle>();
        var competitionState = Substitute.For<ICompetitionStateService>();
        var optimizationService = Substitute.For<IListingOptimizationService>();
        var objectTable = Substitute.For<IObjectTable>();
        var logger = Substitute.For<ILoggerService>();

        var service = new NativeRetainerListHighlighterService(
            addonLifecycle,
            competitionState,
            optimizationService,
            objectTable,
            logger
        );

        addonLifecycle.Received(1).RegisterListener(AddonEvent.PostUpdate, "RetainerList", Arg.Any<IAddonLifecycle.AddonEventDelegate>());

        service.Dispose();

        addonLifecycle.Received(1).UnregisterListener(AddonEvent.PostUpdate, "RetainerList", Arg.Any<IAddonLifecycle.AddonEventDelegate>());
    }
}