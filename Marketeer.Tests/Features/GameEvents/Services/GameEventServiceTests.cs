using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Plugin.Services;
using Marketeer.Features.GameEvents.Services;
using Marketeer.Features.Logging.Contracts;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Features.GameEvents.Services;

public class GameEventServiceTests {
    [Fact]
    public async Task AddonLifecycle_WhenRetainerListOpened_FiresRetainerBellOpenedEvent() {
        // Arrange
        var mockAddonLifecycle = Substitute.For<IAddonLifecycle>();
        var mockLogger = Substitute.For<ILoggerService>();
        var mockFramework = Substitute.For<IFramework>();

        // Ensure the callback executes immediately in the test context
        mockFramework.When(x => x.RunOnFrameworkThread(Arg.Any<Action>())).Do(cb => cb.Arg<Action>()());

        var service = new GameEventService(mockAddonLifecycle, mockLogger, mockFramework);
        var eventFired = false;
        service.RetainerBellOpened += () => eventFired = true;

        // Act
        var call = mockAddonLifecycle.ReceivedCalls()
            .FirstOrDefault(c => c.GetMethodInfo().Name == "RegisterListener" && (string)c.GetArguments()[1]! == "RetainerList");

        Assert.NotNull(call);

        var capturedDelegate = call.GetArguments()[2] as Delegate;
        Assert.NotNull(capturedDelegate);

        capturedDelegate.DynamicInvoke(AddonEvent.PostSetup, null);

        await Task.Delay(600);

        // Assert
        Assert.True(eventFired);
    }

    [Fact]
    public void AddonLifecycle_WhenRetainerSellClosed_FiresRetainerListingAddedEvent() {
        // Arrange
        var mockAddonLifecycle = Substitute.For<IAddonLifecycle>();
        var mockLogger = Substitute.For<ILoggerService>();
        var mockFramework = Substitute.For<IFramework>();

        var service = new GameEventService(mockAddonLifecycle, mockLogger, mockFramework);
        var eventFired = false;
        service.RetainerListingAdded += () => eventFired = true;

        // Act
        var call = mockAddonLifecycle.ReceivedCalls()
            .FirstOrDefault(c => c.GetMethodInfo().Name == "RegisterListener" && (string)c.GetArguments()[1]! == "RetainerSell");

        Assert.NotNull(call);

        var capturedDelegate = call.GetArguments()[2] as Delegate;
        Assert.NotNull(capturedDelegate);

        capturedDelegate.DynamicInvoke(AddonEvent.PreFinalize, null);

        // Assert
        Assert.True(eventFired);
    }
}