using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Marketeer.API.UiInterop.Contracts;
using Marketeer.API.UiInterop.Models;
using Marketeer.Core.Logging.Contracts;
using System;
using System.Collections.Generic;
using System.Text;

namespace Marketeer.API.UiInterop.Services;

public unsafe class NativeWindowService : INativeWindowService {
    private IGameGui gameGui;
    private IWindowHierarchyProvider hierarchyProvider;
    private ILoggerService logger;

    public NativeWindowService(IGameGui gameGui, IWindowHierarchyProvider hierarchyProvider, ILoggerService logger) {
        this.gameGui = gameGui;
        this.hierarchyProvider = hierarchyProvider;
        this.logger = logger;
    }

    public INativeWindow? GetWindow(string name) {
        return new NativeWindow(name, this.DetermineWindowType(name), this.gameGui, this, this.hierarchyProvider, this.logger);
    }

    public INativeWindow? GetFocusedWindow() {
        var atkStage = AtkStage.Instance();
        if (atkStage == null || atkStage->RaptureAtkUnitManager == null) return null;

        var focusedList = atkStage->RaptureAtkUnitManager->AtkUnitManager.FocusedUnitsList;

        for (int i = focusedList.Count - 1; i >= 0; i--) {
            AtkUnitBase* addon = focusedList.Entries[i].Value;

            if (addon != null && addon->IsVisible) {
                var nameSpan = addon->Name;
                var nullIndex = nameSpan.IndexOf((byte)0);
                var actualSpan = nullIndex >= 0 ? nameSpan[..nullIndex] : nameSpan;
                var name = Encoding.UTF8.GetString(actualSpan);

                if (!string.IsNullOrEmpty(name)) return this.GetWindow(name);
            }
        }

        return null;
    }

    public IEnumerable<INativeWindow> GetOpenWindows() {
        var openWindows = new List<INativeWindow>();
        var atkStage = AtkStage.Instance();

        if (atkStage == null || atkStage->RaptureAtkUnitManager == null) return openWindows;

        var loadedUnits = atkStage->RaptureAtkUnitManager->AtkUnitManager.AllLoadedUnitsList;

        for (int i = 0; i < loadedUnits.Count; i++) {
            AtkUnitBase* addon = loadedUnits.Entries[i].Value;

            if (addon != null && addon->IsVisible) {
                var nameSpan = addon->Name;
                var nullIndex = nameSpan.IndexOf((byte)0);
                var actualSpan = nullIndex >= 0 ? nameSpan[..nullIndex] : nameSpan;
                var name = Encoding.UTF8.GetString(actualSpan);

                if (!string.IsNullOrEmpty(name)) {
                    openWindows.Add(new NativeWindow(name, this.DetermineWindowType(name), this.gameGui, this, this.hierarchyProvider, this.logger));
                }
            }
        }

        return openWindows;
    }

    private WindowType DetermineWindowType(string addonName) {
        var lowerName = addonName.ToLowerInvariant();

        if (lowerName.Contains("inventory") || lowerName.Contains("armoury")) return WindowType.Inventory;
        if (lowerName.Contains("selectstring") || lowerName.Contains("selectyesno") || lowerName.Contains("talk")) return WindowType.Dialog;
        if (lowerName.Contains("menu") || lowerName.Contains("context")) return WindowType.Menu;
        if (lowerName.Contains("system") || lowerName.Contains("hud")) return WindowType.System;

        return WindowType.Unknown;
    }
}