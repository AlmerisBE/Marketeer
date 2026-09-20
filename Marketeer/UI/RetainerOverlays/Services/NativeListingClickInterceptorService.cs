using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Plugin.Services;
using Marketeer.Core.Configuration.Contracts;
using Marketeer.Core.Configuration.Models;
using Marketeer.Core.Logging.Contracts;
using Marketeer.Core.RetainerAutomation.Contracts;
using System;

namespace Marketeer.UI.RetainerOverlays.Services;

public class NativeListingClickInterceptorService : IDisposable {
    private IAddonLifecycle addonLifecycle;
    private IHybridAutomationService hybridAutomation;
    private IKeyState keyState;
    private IConfigurationService configService;
    private ILoggerService logger;

    public NativeListingClickInterceptorService(
        IAddonLifecycle addonLifecycle,
        IHybridAutomationService hybridAutomation,
        IKeyState keyState,
        IConfigurationService configService,
        ILoggerService logger) {

        this.addonLifecycle = addonLifecycle;
        this.hybridAutomation = hybridAutomation;
        this.keyState = keyState;
        this.configService = configService;
        this.logger = logger;

        this.addonLifecycle.RegisterListener(AddonEvent.PostReceiveEvent, "RetainerSellList", this.OnReceiveEvent);
    }

    private void OnReceiveEvent(AddonEvent type, AddonArgs args) {
        if (args is AddonReceiveEventArgs receiveArgs) {
            this.EvaluateClick((int)receiveArgs.AtkEventType, receiveArgs.EventParam);
        }
    }

    public void EvaluateClick(int eventType, int eventParam) {
        if (this.hybridAutomation.IsActive) return;

        if (eventType == 35 && eventParam == 0) {
            if (!this.IsModifierPressed()) return;

            this.logger.Info("[ClickInterceptor] Configured modifier + Left-click detected. Arming hybrid automation.");
            this.hybridAutomation.TriggerAdjustment();
        }
    }

    private bool IsModifierPressed() {
        var modifiers = this.configService.GetConfig().AutoSellModifierKey;

        if (modifiers == ModifierKey.None) return true;

        bool requiresCtrl = modifiers.HasFlag(ModifierKey.Ctrl);
        bool requiresShift = modifiers.HasFlag(ModifierKey.Shift);
        bool requiresAlt = modifiers.HasFlag(ModifierKey.Alt);

        bool isCtrlPressed = this.keyState[VirtualKey.CONTROL];
        bool isShiftPressed = this.keyState[VirtualKey.SHIFT];
        bool isAltPressed = this.keyState[VirtualKey.MENU];

        return requiresCtrl == isCtrlPressed &&
               requiresShift == isShiftPressed &&
               requiresAlt == isAltPressed;
    }

    public void Dispose() {
        this.addonLifecycle.UnregisterListener(AddonEvent.PostReceiveEvent, "RetainerSellList", this.OnReceiveEvent);
    }
}