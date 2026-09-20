using Dalamud.Game.ClientState.Keys;
using Dalamud.Plugin.Services;
using Marketeer.Core.Configuration.Contracts;
using Marketeer.Core.Configuration.Models;
using Marketeer.Core.Logging.Contracts;
using Marketeer.Core.RetainerAutomation.Contracts;
using Marketeer.UI.RetainerOverlays.Services;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.UI.RetainerOverlays.Services;

public class NativeListingClickInterceptorServiceTests {
    [Fact]
    public void EvaluateClick_WhenModifiersMatchStrictly_ShouldTriggerAdjustment() {
        var addonLifecycle = Substitute.For<IAddonLifecycle>();
        var hybridService = Substitute.For<IHybridAutomationService>();
        var keyState = Substitute.For<IKeyState>();
        var configService = Substitute.For<IConfigurationService>();
        var logger = Substitute.For<ILoggerService>();

        var config = new PluginConfiguration { AutoSellModifierKey = ModifierKey.Ctrl | ModifierKey.Shift };
        configService.GetConfig().Returns(config);

        keyState[VirtualKey.CONTROL].Returns(true);
        keyState[VirtualKey.SHIFT].Returns(true);
        keyState[VirtualKey.MENU].Returns(false);

        using var service = new NativeListingClickInterceptorService(
            addonLifecycle, hybridService, keyState, configService, logger);

        service.EvaluateClick(35, 0);

        hybridService.Received(1).TriggerAdjustment();
    }

    [Fact]
    public void EvaluateClick_WhenExtraModifierPressed_ShouldNotTriggerAdjustment() {
        var addonLifecycle = Substitute.For<IAddonLifecycle>();
        var hybridService = Substitute.For<IHybridAutomationService>();
        var keyState = Substitute.For<IKeyState>();
        var configService = Substitute.For<IConfigurationService>();
        var logger = Substitute.For<ILoggerService>();

        var config = new PluginConfiguration { AutoSellModifierKey = ModifierKey.Shift };
        configService.GetConfig().Returns(config);

        keyState[VirtualKey.CONTROL].Returns(true); // Extra key pressed
        keyState[VirtualKey.SHIFT].Returns(true);
        keyState[VirtualKey.MENU].Returns(false);

        using var service = new NativeListingClickInterceptorService(
            addonLifecycle, hybridService, keyState, configService, logger);

        service.EvaluateClick(35, 0);

        hybridService.DidNotReceiveWithAnyArgs().TriggerAdjustment();
    }
}