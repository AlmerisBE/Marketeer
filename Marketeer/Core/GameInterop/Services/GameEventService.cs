using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Plugin.Services;
using Marketeer.API.GameInterop.Contracts;
using Marketeer.API.Logging.Contracts;
using System;

namespace Marketeer.Core.GameInterop.Services;

public class GameEventService : IGameEventService, IDisposable {
    private IAddonLifecycle addonLifecycle;
    private IFramework framework;
    private ILoggerService logger;

    private bool isSellListOpen = false;
    private DateTime lastRefreshTime = DateTime.MinValue;

    public event Action? RetainerBellOpened;
    public event Action? RetainerSellListUpdated;

    public GameEventService(IAddonLifecycle addonLifecycle, IFramework framework, ILoggerService logger) {
        this.addonLifecycle = addonLifecycle;
        this.framework = framework;
        this.logger = logger;

        // Hook the main summoning bell list
        this.addonLifecycle.RegisterListener(AddonEvent.PostSetup, "RetainerList", this.OnRetainerListOpened);

        // Hook the opening and closing of the listing window
        this.addonLifecycle.RegisterListener(AddonEvent.PostSetup, "RetainerSellList", this.OnSellListOpened);
        this.addonLifecycle.RegisterListener(AddonEvent.PreFinalize, "RetainerSellList", this.OnSellListClosed);

        // Hook when the user finishes changing a price (the RetainerSell window closes)
        this.addonLifecycle.RegisterListener(AddonEvent.PreFinalize, "RetainerSell", this.OnItemPriceModified);

        this.framework.Update += this.OnFrameworkUpdate;
    }

    private void OnRetainerListOpened(AddonEvent type, AddonArgs args) {
        this.logger.Debug("RetainerList opened. Triggering RetainerBellOpened event.");
        this.RetainerBellOpened?.Invoke();
    }

    private void OnSellListOpened(AddonEvent type, AddonArgs args) {
        this.isSellListOpen = true;
        this.logger.Debug("RetainerSellList opened. Starting systematic refresh.");
        this.RetainerSellListUpdated?.Invoke();
    }

    private void OnSellListClosed(AddonEvent type, AddonArgs args) {
        this.isSellListOpen = false;
        this.logger.Debug("RetainerSellList closed. Stopping refresh.");
    }

    private void OnItemPriceModified(AddonEvent type, AddonArgs args) {
        this.logger.Debug("Retainer item price modified. Forcing immediate refresh.");
        this.RetainerSellListUpdated?.Invoke();
    }

    private void OnFrameworkUpdate(IFramework framework) {
        if (!this.isSellListOpen) {
            return;
        }

        // Throttle to 500ms to catch asynchronous data loading without impacting performance
        if ((DateTime.Now - this.lastRefreshTime).TotalMilliseconds > 500) {
            this.lastRefreshTime = DateTime.Now;
            this.RetainerSellListUpdated?.Invoke();
        }
    }

    public void Dispose() {
        this.addonLifecycle.UnregisterListener(AddonEvent.PostSetup, "RetainerList", this.OnRetainerListOpened);
        this.addonLifecycle.UnregisterListener(AddonEvent.PostSetup, "RetainerSellList", this.OnSellListOpened);
        this.addonLifecycle.UnregisterListener(AddonEvent.PreFinalize, "RetainerSellList", this.OnSellListClosed);
        this.addonLifecycle.UnregisterListener(AddonEvent.PreFinalize, "RetainerSell", this.OnItemPriceModified);

        this.framework.Update -= this.OnFrameworkUpdate;
    }
}