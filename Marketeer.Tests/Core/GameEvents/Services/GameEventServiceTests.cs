using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Plugin.Services;
using Marketeer.API.Logging.Contracts;
using Marketeer.Core.GameInterop.Services;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Core.GameEvents.Services;

public class GameEventServiceTests {
    [Fact]
    public void GameEventService_RegistersAllRequiredAddonListeners() {
        // Arrange
        var mockAddonLifecycle = Substitute.For<IAddonLifecycle>();
        var mockFramework = Substitute.For<IFramework>();
        var mockLogger = Substitute.For<ILoggerService>();
        var mockGameGui = Substitute.For<IGameGui>();

        // Act
        using var service = new GameEventService(mockAddonLifecycle, mockFramework, mockLogger, mockGameGui);

        // Assert
        mockAddonLifecycle.Received().RegisterListener(AddonEvent.PostSetup, "RetainerList", Arg.Any<IAddonLifecycle.AddonEventDelegate>());
        mockAddonLifecycle.Received().RegisterListener(AddonEvent.PostSetup, "RetainerSellList", Arg.Any<IAddonLifecycle.AddonEventDelegate>());
        mockAddonLifecycle.Received().RegisterListener(AddonEvent.PreFinalize, "RetainerSellList", Arg.Any<IAddonLifecycle.AddonEventDelegate>());
        mockAddonLifecycle.Received().RegisterListener(AddonEvent.PreFinalize, "RetainerSell", Arg.Any<IAddonLifecycle.AddonEventDelegate>());
    }

    [Fact]
    public void GameEventService_OnDispose_UnregistersAllListeners() {
        // Arrange
        var mockAddonLifecycle = Substitute.For<IAddonLifecycle>();
        var mockFramework = Substitute.For<IFramework>();
        var mockLogger = Substitute.For<ILoggerService>();
        var mockGameGui = Substitute.For<IGameGui>();

        var service = new GameEventService(mockAddonLifecycle, mockFramework, mockLogger, mockGameGui);

        // Act
        service.Dispose();

        // Assert
        mockAddonLifecycle.Received().UnregisterListener(AddonEvent.PostSetup, "RetainerList", Arg.Any<IAddonLifecycle.AddonEventDelegate>());
        mockAddonLifecycle.Received().UnregisterListener(AddonEvent.PostSetup, "RetainerSellList", Arg.Any<IAddonLifecycle.AddonEventDelegate>());
        mockAddonLifecycle.Received().UnregisterListener(AddonEvent.PreFinalize, "RetainerSellList", Arg.Any<IAddonLifecycle.AddonEventDelegate>());
        mockAddonLifecycle.Received().UnregisterListener(AddonEvent.PreFinalize, "RetainerSell", Arg.Any<IAddonLifecycle.AddonEventDelegate>());
    }
}