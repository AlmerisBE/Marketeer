using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Marketeer.API.GameInterop.Contracts;
using Marketeer.API.Logging.Contracts;
using System;

namespace Marketeer.Core.GameInterop.Services;

public unsafe class GameEventService : IGameEventService, IDisposable {
    private IAddonLifecycle addonLifecycle;
    private IFramework framework;
    private ILoggerService logger;
    private IGameGui gameGui;

    private bool isSellListOpen = false;
    private bool isRetainerSessionActive = false;
    private DateTime lastRefreshTime = DateTime.MinValue;

    public event Action? RetainerBellOpened;
    public event Action? RetainerSellListUpdated;
    public event Action? RetainerSessionStarted;
    public event Action? RetainerSessionEnded;

    public GameEventService(IAddonLifecycle addonLifecycle, IFramework framework, ILoggerService logger, IGameGui gameGui) {
        this.addonLifecycle = addonLifecycle;
        this.framework = framework;
        this.logger = logger;
        this.gameGui = gameGui;

        this.addonLifecycle.RegisterListener(AddonEvent.PostSetup, "RetainerList", this.OnRetainerListOpened);
        this.addonLifecycle.RegisterListener(AddonEvent.PostSetup, "RetainerSellList", this.OnSellListOpened);
        this.addonLifecycle.RegisterListener(AddonEvent.PreFinalize, "RetainerSellList", this.OnSellListClosed);
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

    private void OnFrameworkUpdate(IFramework frameworkInstance) {
        this.CheckRetainerSessionState();

        if (!this.isSellListOpen) {
            return;
        }

        if ((DateTime.Now - this.lastRefreshTime).TotalMilliseconds > 500) {
            this.lastRefreshTime = DateTime.Now;
            this.RetainerSellListUpdated?.Invoke();
        }
    }

    private void CheckRetainerSessionState() {
        bool isSessionActive = false;

        // 1. Check if the root Summoning Bell menu is visible
        var addonPtr = this.gameGui.GetAddonByName("RetainerList");
        if (addonPtr.Address != IntPtr.Zero) {
            var addon = (AtkUnitBase*)addonPtr.Address;
            if (addon->IsVisible) {
                isSessionActive = true;
            }
        }

        // 2. If the menu is hidden, check if a retainer is actively summoned in memory
        if (!isSessionActive) {
            var manager = RetainerManager.Instance();
            if (manager != null) {
                var activeRetainer = manager->GetActiveRetainer();
                if (activeRetainer != null && activeRetainer->RetainerId != 0u) {
                    isSessionActive = true;
                }
            }
        }

        // Trigger state change events
        if (isSessionActive && !this.isRetainerSessionActive) {
            this.isRetainerSessionActive = true;
            this.logger.Debug("Retainer session started.");
            this.RetainerSessionStarted?.Invoke();
        }
        else if (!isSessionActive && this.isRetainerSessionActive) {
            this.isRetainerSessionActive = false;
            this.logger.Debug("Retainer session ended.");
            this.RetainerSessionEnded?.Invoke();
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