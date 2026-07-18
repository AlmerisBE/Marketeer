using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Marketeer.Features.Logging.Contracts;
using Marketeer.Features.RetainerAutomation.Contracts;
using System;

namespace Marketeer.Features.RetainerAutomation.Services;

public unsafe class UiInteractionService : IUiInteractionService {
    private IGameGui gameGui;
    private ILoggerService logger;

    public UiInteractionService(IGameGui gameGui, ILoggerService logger) {
        this.gameGui = gameGui;
        this.logger = logger;
    }

    public bool IsAddonReady(string addonName) {
        var addonPtr = this.gameGui.GetAddonByName(addonName);
        if (addonPtr.Address == IntPtr.Zero) {
            return false;
        }

        var addon = (AtkUnitBase*)addonPtr.Address;
        return addon->IsVisible && addon->UldManager.LoadedState == AtkLoadState.Loaded;
    }

    public void SelectRetainer(int index) {
        var addonPtr = this.gameGui.GetAddonByName("RetainerList");
        if (addonPtr.Address == IntPtr.Zero) {
            return;
        }

        var addon = (AtkUnitBase*)addonPtr.Address;
        this.logger.Debug($"UiInteractionService: Firing callback to select retainer at index {index}.");

        var values = stackalloc AtkValue[2];
        values[0].Type = AtkValueType.Int;
        values[0].Int = 2;
        values[1].Type = AtkValueType.Int;
        values[1].Int = index;

        addon->FireCallback(2, values);
    }

    public void OpenRetainerMarket() {
        var addonPtr = this.gameGui.GetAddonByName("SelectString");
        if (addonPtr.Address == IntPtr.Zero) {
            return;
        }

        var addon = (AtkUnitBase*)addonPtr.Address;
        this.logger.Debug("UiInteractionService: Firing callback to open market listings.");

        var values = stackalloc AtkValue[2];
        values[0].Type = AtkValueType.Int;
        values[0].Int = 0;

        // Target index 5: "Sell items in retainer's inventory on the market" (when ventures are active).
        // Note: Change this to 3 if you test on low-level retainers without venture options.
        values[1].Type = AtkValueType.Int;
        values[1].Int = 5;

        addon->FireCallback(2, values);
    }

    public void CloseRetainerMarket() {
        var addonPtr = this.gameGui.GetAddonByName("RetainerSell");
        if (addonPtr.Address == IntPtr.Zero) {
            return;
        }

        var addon = (AtkUnitBase*)addonPtr.Address;
        this.logger.Debug("UiInteractionService: Firing cancel callback to close RetainerSell window.");

        var values = stackalloc AtkValue[1];
        values[0].Type = AtkValueType.Int;
        values[0].Int = -1; // -1 represents the universal Close/Cancel event for AtkUnitBase

        addon->FireCallback(1, values);
    }

    public void CloseSelectString() {
        var addonPtr = this.gameGui.GetAddonByName("SelectString");
        if (addonPtr.Address == IntPtr.Zero) {
            return;
        }

        var addon = (AtkUnitBase*)addonPtr.Address;
        this.logger.Debug("UiInteractionService: Firing cancel callback to close SelectString menu.");

        var values = stackalloc AtkValue[1];
        values[0].Type = AtkValueType.Int;
        values[0].Int = -1;

        addon->FireCallback(1, values);
    }
}