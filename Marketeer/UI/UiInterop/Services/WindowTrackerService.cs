using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Marketeer.Core.Logging.Contracts;
using Marketeer.UI.UiInterop.Contracts;
using System;
using System.Text;

namespace Marketeer.UI.UiInterop.Services;

public unsafe class WindowTrackerService : IWindowTrackerService, IDisposable {
    private IAddonLifecycle addonLifecycle;
    private IFramework framework;
    private ILoggerService logger;
    private string? lastFocusedWindowName;

    public bool IsTracking { get; private set; }

    public WindowTrackerService(IAddonLifecycle addonLifecycle, IFramework framework, ILoggerService logger) {
        this.addonLifecycle = addonLifecycle;
        this.framework = framework;
        this.logger = logger;
    }

    public void EnableTracking() {
        if (this.IsTracking) return;

        this.logger.Debug("WindowTrackerService: Enabling native window tracking.");
        this.addonLifecycle.RegisterListener(AddonEvent.PostSetup, this.OnWindowOpened);
        this.addonLifecycle.RegisterListener(AddonEvent.PreFinalize, this.OnWindowClosed);
        this.framework.Update += this.OnFrameworkUpdate;
        this.IsTracking = true;
    }

    public void DisableTracking() {
        if (!this.IsTracking) return;

        this.logger.Debug("WindowTrackerService: Disabling native window tracking.");
        this.addonLifecycle.UnregisterListener(AddonEvent.PostSetup, this.OnWindowOpened);
        this.addonLifecycle.UnregisterListener(AddonEvent.PreFinalize, this.OnWindowClosed);
        this.framework.Update -= this.OnFrameworkUpdate;
        this.IsTracking = false;
        this.lastFocusedWindowName = null;
    }

    private void OnWindowOpened(AddonEvent type, AddonArgs args) {
        var context = this.GetVisibleWindowsContext();
        this.logger.Debug($"[Window Tracker] Opened: {args.AddonName} | Context (Visible): [{context}]");
    }

    private void OnWindowClosed(AddonEvent type, AddonArgs args) {
        var context = this.GetVisibleWindowsContext();
        this.logger.Debug($"[Window Tracker] Closed: {args.AddonName} | Context (Remaining): [{context}]");
    }

    private string GetVisibleWindowsContext() {
        var atkStage = AtkStage.Instance();
        if (atkStage == null || atkStage->RaptureAtkUnitManager == null) return string.Empty;

        var loadedUnits = atkStage->RaptureAtkUnitManager->AtkUnitManager.AllLoadedUnitsList;
        var visibleNames = new System.Collections.Generic.List<string>();

        for (int i = 0; i < loadedUnits.Count; i++) {
            AtkUnitBase* addon = loadedUnits.Entries[i].Value;

            if (addon != null && addon->IsVisible) {
                var nameSpan = addon->Name;
                var nullIndex = nameSpan.IndexOf((byte)0);
                var actualSpan = nullIndex >= 0 ? nameSpan[..nullIndex] : nameSpan;
                var name = Encoding.UTF8.GetString(actualSpan);

                if (!string.IsNullOrEmpty(name)) visibleNames.Add(name);
            }
        }

        return string.Join(", ", visibleNames);
    }

    private void OnFrameworkUpdate(IFramework frameworkInstance) {
        var atkStage = AtkStage.Instance();
        if (atkStage == null || atkStage->RaptureAtkUnitManager == null) return;

        var focusedUnits = atkStage->RaptureAtkUnitManager->AtkUnitManager.FocusedUnitsList;

        if (focusedUnits.Count > 0) {
            AtkUnitBase* addon = focusedUnits.Entries[0].Value;

            if (addon != null) {
                var nameSpan = addon->Name;
                var nullIndex = nameSpan.IndexOf((byte)0);
                var actualSpan = nullIndex >= 0 ? nameSpan[..nullIndex] : nameSpan;
                var currentFocusedName = Encoding.UTF8.GetString(actualSpan);

                if (currentFocusedName != this.lastFocusedWindowName && !string.IsNullOrEmpty(currentFocusedName)) {
                    this.lastFocusedWindowName = currentFocusedName;
                    this.logger.Debug($"[Window Tracker] Focused: {currentFocusedName}");
                }
            }
        }
        else if (this.lastFocusedWindowName != null) {
            this.logger.Debug($"[Window Tracker] Focus Lost: {this.lastFocusedWindowName}");
            this.lastFocusedWindowName = null;
        }
    }

    public void Dispose() {
        this.DisableTracking();
    }
}