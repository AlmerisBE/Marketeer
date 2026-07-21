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
        var values = stackalloc AtkValue[2];
        values[0].Type = AtkValueType.Int;
        values[0].Int = 2;
        values[1].Type = AtkValueType.Int;
        values[1].Int = index;

        addon->FireCallback(2, values, true);
    }

    public void OpenRetainerMarket() {
        var addonPtr = this.gameGui.GetAddonByName("SelectString");
        if (addonPtr.Address == IntPtr.Zero) {
            return;
        }

        var addon = (AtkUnitBase*)addonPtr.Address;
        var values = stackalloc AtkValue[2];
        values[0].Type = AtkValueType.Int;
        values[0].Int = 0;
        values[1].Type = AtkValueType.Int;
        values[1].Int = 5;

        addon->FireCallback(2, values, true);
    }

    public void CloseRetainerMarket() {
        var addonPtr = this.gameGui.GetAddonByName("RetainerSell");
        if (addonPtr.Address == IntPtr.Zero) {
            return;
        }

        var addon = (AtkUnitBase*)addonPtr.Address;
        var values = stackalloc AtkValue[1];
        values[0].Type = AtkValueType.Int;
        values[0].Int = -1;

        addon->FireCallback(1, values, true);
    }

    public void CloseSelectString() {
        var addonPtr = this.gameGui.GetAddonByName("SelectString");
        if (addonPtr.Address == IntPtr.Zero) {
            return;
        }

        var addon = (AtkUnitBase*)addonPtr.Address;
        var values = stackalloc AtkValue[1];
        values[0].Type = AtkValueType.Int;
        values[0].Int = -1;

        addon->FireCallback(1, values, true);
    }

    public void SelectItemInSellList(int uiIndex) {
        var addonPtr = this.gameGui.GetAddonByName("RetainerSellList");
        if (addonPtr.Address == IntPtr.Zero) {
            return;
        }

        var addon = (AtkUnitBase*)addonPtr.Address;
        this.logger.Debug($"UiInteractionService: Firing callback to select UI index {uiIndex}.");

        var values = stackalloc AtkValue[3];
        values[0].Type = AtkValueType.Int;
        values[0].Int = 0;
        values[1].Type = AtkValueType.Int;
        values[1].Int = uiIndex;
        values[2].Type = AtkValueType.Int;
        values[2].Int = 0;

        addon->FireCallback(3, values, true);
    }

    public void SelectContextMenuItem(int index) {
        var addonPtr = this.gameGui.GetAddonByName("ContextMenu");
        if (addonPtr.Address == IntPtr.Zero) {
            return;
        }

        var addon = (AtkUnitBase*)addonPtr.Address;
        this.logger.Debug($"UiInteractionService: Firing callback to select ContextMenu index {index}.");

        var values = stackalloc AtkValue[5];
        values[0].Type = AtkValueType.Int;
        values[0].Int = 0;
        values[1].Type = AtkValueType.Int;
        values[1].Int = index;
        values[2].Type = AtkValueType.Int;
        values[2].Int = 0;
        values[3].Type = AtkValueType.Int;
        values[3].Int = 0;
        values[4].Type = AtkValueType.Int;
        values[4].Int = 0;

        addon->FireCallback(5, values, true);
    }

    public void ConfirmPriceUpdate(uint newPrice) {
        var addonPtr = this.gameGui.GetAddonByName("RetainerSell");
        if (addonPtr.Address == IntPtr.Zero) {
            return;
        }

        var addon = (AtkUnitBase*)addonPtr.Address;
        this.logger.Debug($"UiInteractionService: Firing callback to update price to {newPrice}.");

        var values = stackalloc AtkValue[2];
        values[0].Type = AtkValueType.Int;
        values[0].Int = 0;
        values[1].Type = AtkValueType.UInt;
        values[1].UInt = newPrice;

        addon->FireCallback(2, values, true);
    }
}