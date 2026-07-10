using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Plugin.Services;
using Marketeer.Features.GameEvents.Contracts;
using Marketeer.Features.Logging.Contracts;
using System;
using System.Threading.Tasks;

namespace Marketeer.Features.GameEvents.Services;

public class GameEventService : IGameEventService, IDisposable {
    private ICondition condition;
    private IAddonLifecycle addonLifecycle;
    private ILoggerService logger;
    private IFramework framework;

    public event Action? RetainerBellOpened;
    public event Action? RetainerListingsOpened;
    public event Action? RetainerListingAdded;
    public event Action? RetainerMainMenuOpened;

    public GameEventService(ICondition condition, IAddonLifecycle addonLifecycle, ILoggerService logger, IFramework framework) {
        this.condition = condition;
        this.addonLifecycle = addonLifecycle;
        this.logger = logger;
        this.framework = framework;

        this.condition.ConditionChange += this.OnConditionChange;

        // Changed to PostRefresh to ensure UI nodes are populated with text data before parsing
        this.addonLifecycle.RegisterListener(AddonEvent.PostRefresh, "RetainerSellList", this.OnRetainerSellListOpened);
        this.addonLifecycle.RegisterListener(AddonEvent.PreFinalize, "RetainerSell", this.OnRetainerSellClosed);
        this.addonLifecycle.RegisterListener(AddonEvent.PostSetup, "SelectString", this.OnSelectStringOpened);
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

    private async void OnSelectStringOpened(AddonEvent type, AddonArgs args) {
        await Task.Delay(1000);
        await this.framework.RunOnFrameworkThread(() => {
            this.logger.Debug("SelectString dialog delayed sync triggered.");
            this.RetainerMainMenuOpened?.Invoke();
        });
    }

    public void Dispose() {
        this.condition.ConditionChange -= this.OnConditionChange;
        this.addonLifecycle.UnregisterListener(AddonEvent.PostRefresh, "RetainerSellList", this.OnRetainerSellListOpened);
        this.addonLifecycle.UnregisterListener(AddonEvent.PreFinalize, "RetainerSell", this.OnRetainerSellClosed);
        this.addonLifecycle.UnregisterListener(AddonEvent.PostSetup, "SelectString", this.OnSelectStringOpened);
    }
}