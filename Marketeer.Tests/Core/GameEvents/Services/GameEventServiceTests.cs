using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Plugin.Services;
using Marketeer.API.GameInterop.Services;
using Marketeer.Core.Logging.Contracts;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Core.GameEvents.Services;

public class GameEventServiceTests {
    [Fact]
    public void GameEventService_RegistersAllRequiredAddonListeners() {
        var addonLifecycle = Substitute.For<IAddonLifecycle>();
        var framework = Substitute.For<IFramework>();
        var logger = Substitute.For<ILoggerService>();
        var gameGui = Substitute.For<IGameGui>();

        var service = new GameEventService(addonLifecycle, framework, logger, gameGui);

        // Verify that only the strictly necessary and optimized listeners are registered
        addonLifecycle.Received(1).RegisterListener(AddonEvent.PostSetup, "RetainerList", Arg.Any<IAddonLifecycle.AddonEventDelegate>());
        addonLifecycle.Received(1).RegisterListener(AddonEvent.PostSetup, "RetainerSellList", Arg.Any<IAddonLifecycle.AddonEventDelegate>());
        addonLifecycle.Received(1).RegisterListener(AddonEvent.PreFinalize, "RetainerSell", Arg.Any<IAddonLifecycle.AddonEventDelegate>());
    }

    [Fact]
    public void GameEventService_OnDispose_UnregistersAllListeners() {
        var addonLifecycle = Substitute.For<IAddonLifecycle>();
        var framework = Substitute.For<IFramework>();
        var logger = Substitute.For<ILoggerService>();
        var gameGui = Substitute.For<IGameGui>();

        var service = new GameEventService(addonLifecycle, framework, logger, gameGui);
        service.Dispose();

        addonLifecycle.Received(1).UnregisterListener(AddonEvent.PostSetup, "RetainerList", Arg.Any<IAddonLifecycle.AddonEventDelegate>());
        addonLifecycle.Received(1).UnregisterListener(AddonEvent.PostSetup, "RetainerSellList", Arg.Any<IAddonLifecycle.AddonEventDelegate>());
        addonLifecycle.Received(1).UnregisterListener(AddonEvent.PreFinalize, "RetainerSell", Arg.Any<IAddonLifecycle.AddonEventDelegate>());
    }
}