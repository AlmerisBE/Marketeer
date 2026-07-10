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
        var mockLogger = Substitute.For<ILoggerService>();

        var service = new GameEventService(mockCondition, mockLogger);
        var eventFired = false;
        service.RetainerBellOpened += () => eventFired = true;

        // Act
        // Utilisation du délégué explicite ICondition.ConditionChangeDelegate
        mockCondition.ConditionChange += Raise.Event<ICondition.ConditionChangeDelegate>(ConditionFlag.OccupiedSummoningBell, true);

        // Assert
        Assert.True(eventFired);
    }

    [Fact]
    public void ConditionChange_WhenSummoningBellClosed_DoesNotFireEvent() {
        // Arrange
        var mockCondition = Substitute.For<ICondition>();
        var mockLogger = Substitute.For<ILoggerService>();

        var service = new GameEventService(mockCondition, mockLogger);
        var eventFired = false;
        service.RetainerBellOpened += () => eventFired = true;

        // Act
        // Utilisation du délégué explicite ICondition.ConditionChangeDelegate
        mockCondition.ConditionChange += Raise.Event<ICondition.ConditionChangeDelegate>(ConditionFlag.OccupiedSummoningBell, false);

        // Assert
        Assert.False(eventFired);
    }
}