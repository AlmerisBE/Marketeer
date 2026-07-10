using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Plugin.Services;
using Marketeer.Features.GameEvents.Services;
using Marketeer.Features.Logging.Contracts;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Features.GameEvents.Services;

public class GameEventServiceTests {
    [Fact]
    public void ConditionChange_WhenSummoningBellOpened_FiresRetainerBellOpenedEvent() {
        // Arrange
        var mockCondition = Substitute.For<ICondition>();
        var mockAddonLifecycle = Substitute.For<IAddonLifecycle>();
        var mockLogger = Substitute.For<ILoggerService>();

        var service = new GameEventService(mockCondition, mockAddonLifecycle, mockLogger);
        var eventFired = false;
        service.RetainerBellOpened += () => eventFired = true;

        // Act
        mockCondition.ConditionChange += Raise.Event<ICondition.ConditionChangeDelegate>(ConditionFlag.OccupiedSummoningBell, true);

        // Assert
        Assert.True(eventFired);
    }

    [Fact]
    public void AddonLifecycle_WhenRetainerSellListOpened_FiresRetainerListingsOpenedEvent() {
        // Arrange
        var mockCondition = Substitute.For<ICondition>();
        var mockAddonLifecycle = Substitute.For<IAddonLifecycle>();
        var mockLogger = Substitute.For<ILoggerService>();

        var service = new GameEventService(mockCondition, mockAddonLifecycle, mockLogger);
        var eventFired = false;
        service.RetainerListingsOpened += () => eventFired = true;

        // Act
        // Match the updated addon name: "RetainerSellList"
        var call = mockAddonLifecycle.ReceivedCalls()
            .FirstOrDefault(c => c.GetMethodInfo().Name == "RegisterListener" && (string)c.GetArguments()[1]! == "RetainerSellList");

        Assert.NotNull(call);

        var capturedDelegate = call.GetArguments()[2] as Delegate;
        Assert.NotNull(capturedDelegate);

        capturedDelegate.DynamicInvoke(AddonEvent.PostSetup, null);

        // Assert
        Assert.True(eventFired);
    }

    [Fact]
    public void AddonLifecycle_WhenRetainerSellClosed_FiresRetainerListingAddedEvent() {
        // Arrange
        var mockCondition = Substitute.For<ICondition>();
        var mockAddonLifecycle = Substitute.For<IAddonLifecycle>();
        var mockLogger = Substitute.For<ILoggerService>();

        var service = new GameEventService(mockCondition, mockAddonLifecycle, mockLogger);
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