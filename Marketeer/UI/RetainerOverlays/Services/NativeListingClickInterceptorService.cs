using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Plugin.Services;
using Marketeer.Core.Logging.Contracts;
using Marketeer.Core.RetainerAutomation.Contracts;
using System;

namespace Marketeer.UI.RetainerOverlays.Services;

public class NativeListingClickInterceptorService : IDisposable {
    private IAddonLifecycle addonLifecycle;
    private IHybridAutomationService hybridAutomation;
    private IKeyState keyState;
    private ILoggerService logger;

    public NativeListingClickInterceptorService(
        IAddonLifecycle addonLifecycle,
        IHybridAutomationService hybridAutomation,
        IKeyState keyState,
        ILoggerService logger) {

        this.addonLifecycle = addonLifecycle;
        this.hybridAutomation = hybridAutomation;
        this.keyState = keyState;
        this.logger = logger;

        this.addonLifecycle.RegisterListener(AddonEvent.PostReceiveEvent, "RetainerSellList", this.OnReceiveEvent);
    }

    private void OnReceiveEvent(AddonEvent type, AddonArgs args) {
        if (this.hybridAutomation.IsActive) return;

        if (args is AddonReceiveEventArgs receiveArgs && (int)receiveArgs.AtkEventType == 35) {
            bool isShiftHeld = this.keyState[VirtualKey.SHIFT] || this.keyState[VirtualKey.LSHIFT] || this.keyState[VirtualKey.RSHIFT];

            if (!isShiftHeld) return;

            // EventParam 0 corresponds to a native Left-Click action.
            if (receiveArgs.EventParam == 0) {
                this.logger.Info("[ClickInterceptor] Shift + Left-click detected. Arming hybrid automation.");
                this.hybridAutomation.TriggerAdjustment();
            }
        }
    }

    public void Dispose() {
        this.addonLifecycle.UnregisterListener(AddonEvent.PostReceiveEvent, "RetainerSellList", this.OnReceiveEvent);
    }
}