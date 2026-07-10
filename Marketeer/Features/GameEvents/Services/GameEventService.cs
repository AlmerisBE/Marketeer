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
    public event Action? RetainerListingAdded;

    public GameEventService(ICondition condition, IAddonLifecycle addonLifecycle, ILoggerService logger) {
        this.condition = condition;
        this.addonLifecycle = addonLifecycle;
        this.logger = logger;

        this.condition.ConditionChange += this.OnConditionChange;

        // Changed to PostRefresh to ensure UI nodes are populated with text data before parsing
        this.addonLifecycle.RegisterListener(AddonEvent.PostRefresh, "RetainerSellList", this.OnRetainerSellListOpened);
        this.addonLifecycle.RegisterListener(AddonEvent.PreFinalize, "RetainerSell", this.OnRetainerSellClosed);
    }

    private void OnConditionChange(ConditionFlag flag, bool value) {
        if (flag == ConditionFlag.OccupiedSummoningBell && value) {
            this.logger.Debug("Retainer bell interaction detected.");
            this.RetainerBellOpened?.Invoke();
        }
    }

    private void OnRetainerSellListOpened(AddonEvent type, AddonArgs args) {
        this.logger.Debug("Retainer sell list addon populated and refreshed.");
        this.RetainerListingsOpened?.Invoke();
    }

    private void OnRetainerSellClosed(AddonEvent type, AddonArgs args) {
        this.logger.Debug("Retainer sell addon closed (listing added or modified).");
        this.RetainerListingAdded?.Invoke();
    }

    public void Dispose() {
        this.condition.ConditionChange -= this.OnConditionChange;
        this.addonLifecycle.UnregisterListener(AddonEvent.PostRefresh, "RetainerSellList", this.OnRetainerSellListOpened);
        this.addonLifecycle.UnregisterListener(AddonEvent.PreFinalize, "RetainerSell", this.OnRetainerSellClosed);
    }
}