using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Marketeer.API.GameInterop.Contracts;
using Marketeer.Core.Logging.Contracts;
using System;

namespace Marketeer.API.GameInterop.Services;

public unsafe class GameEventService : IGameEventService, IDisposable {
    private IAddonLifecycle addonLifecycle;
    private IFramework framework;
    private ILoggerService logger;
    private IGameGui gameGui;

    private bool isRetainerSessionActive = false;

    public event Action? RetainerBellOpened;
    public event Action? RetainerSellListUpdated;
    public event Action? RetainerSessionStarted;
    public event Action? RetainerSessionEnded;

    public GameEventService(IAddonLifecycle addonLifecycle, IFramework framework, ILoggerService logger, IGameGui gameGui) {
        this.addonLifecycle = addonLifecycle;
        this.framework = framework;
        this.logger = logger;
        this.gameGui = gameGui;

        // Using precise events instead of 500ms polling reduces memory stress and UI lag
        this.addonLifecycle.RegisterListener(AddonEvent.PostSetup, "RetainerList", this.OnRetainerListOpened);
        this.addonLifecycle.RegisterListener(AddonEvent.PostSetup, "RetainerSellList", this.OnSellListStateChanged);
        this.addonLifecycle.RegisterListener(AddonEvent.PreFinalize, "RetainerSell", this.OnItemPriceModified);

        this.framework.Update += this.OnFrameworkUpdate;
    }

    private void OnRetainerListOpened(AddonEvent type, AddonArgs args) {
        this.logger.Debug("RetainerList opened. Triggering RetainerBellOpened event.");
        this.RetainerBellOpened?.Invoke();
    }

    private void OnSellListStateChanged(AddonEvent type, AddonArgs args) {
        this.logger.Debug("RetainerSellList state updated natively. Emitting event.");
        this.RetainerSellListUpdated?.Invoke();
    }

    private void OnItemPriceModified(AddonEvent type, AddonArgs args) {
        this.logger.Debug("Retainer item price modified natively. Forcing immediate list refresh.");
        this.RetainerSellListUpdated?.Invoke();
    }

    private void OnFrameworkUpdate(IFramework frameworkInstance) {
        this.CheckRetainerSessionState();
    }

    private void CheckRetainerSessionState() {
        bool isSessionActive = false;

        var addonPtr = this.gameGui.GetAddonByName("RetainerList");
        if (addonPtr.Address != IntPtr.Zero) {
            var addon = (AtkUnitBase*)addonPtr.Address;
            if (addon->IsVisible) {
                isSessionActive = true;
            }
        }

        if (!isSessionActive) {
            var manager = RetainerManager.Instance();
            if (manager != null) {
                var activeRetainer = manager->GetActiveRetainer();
                if (activeRetainer != null && activeRetainer->RetainerId != 0u) {
                    isSessionActive = true;
                }
            }
        }

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
        this.addonLifecycle.UnregisterListener(AddonEvent.PostSetup, "RetainerSellList", this.OnSellListStateChanged);
        this.addonLifecycle.UnregisterListener(AddonEvent.PreFinalize, "RetainerSell", this.OnItemPriceModified);

        this.framework.Update -= this.OnFrameworkUpdate;
    }
}