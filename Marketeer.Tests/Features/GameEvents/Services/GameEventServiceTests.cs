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
    public void AddonLifecycle_WhenRetainerSellOpened_FiresRetainerListingsOpenedEvent() {
        // Arrange
        var mockCondition = Substitute.For<ICondition>();
        var mockAddonLifecycle = Substitute.For<IAddonLifecycle>();
        var mockLogger = Substitute.For<ILoggerService>();

        var service = new GameEventService(mockCondition, mockAddonLifecycle, mockLogger);
        var eventFired = false;
        service.RetainerListingsOpened += () => eventFired = true;

        // Act
        // Use NSubstitute's ReceivedCalls to dynamically find the registered delegate
        var call = mockAddonLifecycle.ReceivedCalls()
            .FirstOrDefault(c => c.GetMethodInfo().Name == "RegisterListener" && (string)c.GetArguments()[1]! == "RetainerSell");

        Assert.NotNull(call);

        var capturedDelegate = call.GetArguments()[2] as Delegate;
        Assert.NotNull(capturedDelegate);

        // Since our method doesn't use the AddonArgs parameter, we can safely pass null 
        // instead of trying to mock a class without a parameterless constructor.
        capturedDelegate.DynamicInvoke(AddonEvent.PostSetup, null);

        // Assert
        Assert.True(eventFired);
    }
}