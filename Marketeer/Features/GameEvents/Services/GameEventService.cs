using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Plugin.Services;
using Marketeer.Features.GameEvents.Contracts;
using Marketeer.Features.Logging.Contracts;
using System;
using System.Threading.Tasks;

namespace Marketeer.Features.GameEvents.Services;

public class GameEventService : IGameEventService, IDisposable {
    private IAddonLifecycle addonLifecycle;
    private ILoggerService logger;
    private IFramework framework;

    private bool isSellListOpen = false;
    private DateTime lastScanTime = DateTime.MinValue;

    public event Action? RetainerBellOpened;
    public event Action? RetainerListingsOpened;
    public event Action? RetainerListingAdded;

    public GameEventService(IAddonLifecycle addonLifecycle, ILoggerService logger, IFramework framework) {
        this.addonLifecycle = addonLifecycle;
        this.logger = logger;
        this.framework = framework;

        // Bypassing ICondition to listen directly to FFXIV's UI initialization
        this.addonLifecycle.RegisterListener(AddonEvent.PostSetup, "RetainerList", this.OnRetainerListOpened);
        this.addonLifecycle.RegisterListener(AddonEvent.PostSetup, "RetainerSellList", this.OnRetainerSellListOpened);
        this.addonLifecycle.RegisterListener(AddonEvent.PreFinalize, "RetainerSellList", this.OnRetainerSellListClosed);
        this.addonLifecycle.RegisterListener(AddonEvent.PreFinalize, "RetainerSell", this.OnRetainerSellClosed);

        this.framework.Update += this.OnFrameworkUpdate;
    }

    private async void OnRetainerListOpened(AddonEvent type, AddonArgs args) {
        // 500ms network propagation delay to ensure memory structs are filled
        await Task.Delay(500);
        await this.framework.RunOnFrameworkThread(() => {
            this.logger.Debug("RetainerList addon delayed sync triggered.");
            this.RetainerBellOpened?.Invoke();
        });
    }

    private async void OnRetainerSellListOpened(AddonEvent type, AddonArgs args) {
        this.isSellListOpen = true;
        await Task.Delay(500);
        await this.framework.RunOnFrameworkThread(() => {
            this.logger.Debug("Retainer sell list delayed sync triggered.");
            this.RetainerListingsOpened?.Invoke();
        });
    }

    private void OnRetainerSellListClosed(AddonEvent type, AddonArgs args) {
        this.isSellListOpen = false;
    }

    private void OnRetainerSellClosed(AddonEvent type, AddonArgs args) {
        this.logger.Debug("Retainer sell addon closed (listing added or modified).");
        this.RetainerListingAdded?.Invoke();
    }

    private void OnFrameworkUpdate(IFramework framework) {
        // Executes twice a second while UI is actively open to capture scrolling events
        if (this.isSellListOpen && (DateTime.Now - this.lastScanTime).TotalMilliseconds > 500) {
            this.lastScanTime = DateTime.Now;
            this.RetainerListingsOpened?.Invoke();
        }
    }

    public void Dispose() {
        this.addonLifecycle.UnregisterListener(AddonEvent.PostSetup, "RetainerList", this.OnRetainerListOpened);
        this.addonLifecycle.UnregisterListener(AddonEvent.PostSetup, "RetainerSellList", this.OnRetainerSellListOpened);
        this.addonLifecycle.UnregisterListener(AddonEvent.PreFinalize, "RetainerSellList", this.OnRetainerSellListClosed);
        this.addonLifecycle.UnregisterListener(AddonEvent.PreFinalize, "RetainerSell", this.OnRetainerSellClosed);

        this.framework.Update -= this.OnFrameworkUpdate;
    }
}