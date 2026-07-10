using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Plugin.Services;
using Marketeer.Features.GameEvents.Contracts;
using Marketeer.Features.Logging.Contracts;
using System;

namespace Marketeer.Features.GameEvents.Services;

public class GameEventService : IGameEventService, IDisposable {
    private ICondition condition;
    private IAddonLifecycle addonLifecycle;
    private ILoggerService logger;

    public event Action? RetainerBellOpened;
    public event Action? RetainerListingsOpened;

    public GameEventService(ICondition condition, IAddonLifecycle addonLifecycle, ILoggerService logger) {
        this.condition = condition;
        this.addonLifecycle = addonLifecycle;
        this.logger = logger;

        this.condition.ConditionChange += this.OnConditionChange;
        this.addonLifecycle.RegisterListener(AddonEvent.PostSetup, "RetainerSell", this.OnRetainerSellOpened);
    }

    private void OnConditionChange(ConditionFlag flag, bool value) {
        if (flag == ConditionFlag.OccupiedSummoningBell && value) {
            this.logger.Debug("Retainer bell interaction detected.");
            this.RetainerBellOpened?.Invoke();
        }
    }

    private void OnRetainerSellOpened(AddonEvent type, AddonArgs args) {
        this.logger.Debug("Retainer sell list addon opened.");
        this.RetainerListingsOpened?.Invoke();
    }

    public void Dispose() {
        this.condition.ConditionChange -= this.OnConditionChange;
        this.addonLifecycle.UnregisterListener(AddonEvent.PostSetup, "RetainerSell", this.OnRetainerSellOpened);
    }
}