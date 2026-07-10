using Dalamud.Game.ClientState.Conditions;
using Dalamud.Plugin.Services;
using Marketeer.Features.GameEvents.Contracts;
using Marketeer.Features.Logging.Contracts;
using System;

namespace Marketeer.Features.GameEvents.Services;

public class GameEventService : IGameEventService, IDisposable {
    private ICondition condition;
    private ILoggerService logger;

    public event Action? RetainerBellOpened;

    public GameEventService(ICondition condition, ILoggerService logger) {
        this.condition = condition;
        this.logger = logger;

        this.condition.ConditionChange += this.OnConditionChange;
    }

    private void OnConditionChange(ConditionFlag flag, bool value) {
        if (flag == ConditionFlag.OccupiedSummoningBell && value) {
            this.logger.Debug("Retainer bell interaction detected.");
            this.RetainerBellOpened?.Invoke();
        }
    }

    public void Dispose() {
        this.condition.ConditionChange -= this.OnConditionChange;
    }
}