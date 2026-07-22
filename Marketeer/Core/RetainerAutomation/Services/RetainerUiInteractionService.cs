using Dalamud.Memory;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Marketeer.API.Logging.Contracts;
using Marketeer.API.RetainerAutomation.Contracts;
using Marketeer.API.UiInterop.Contracts;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Marketeer.Core.RetainerAutomation.Services;

public unsafe class RetainerUiInteractionService : IRetainerUiInteractionService {
    private IGameGui gameGui;
    private INativeWindowService windowService;
    private ILoggerService logger;

    public RetainerUiInteractionService(IGameGui gameGui, INativeWindowService windowService, ILoggerService logger) {
        this.gameGui = gameGui;
        this.windowService = windowService;
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

    public bool IsRetainerAvailable(string retainerName) {
        var window = this.windowService.GetWindow("RetainerList");
        if (window == null || !window.IsVisible) {
            return false;
        }

        var pattern = $@"(?:^|\|\s*){Regex.Escape(retainerName)}$";
        return window.GetElements()
            .Where(e => e.Type == NativeUiElementType.Button && e.Text.Contains(" | "))
            .Any(e => Regex.IsMatch(e.Text, pattern, RegexOptions.IgnoreCase));
    }

    public bool SelectRetainer(string retainerName) {
        var window = this.windowService.GetWindow("RetainerList");
        if (window == null || !window.IsVisible) {
            this.logger.Warning("Cannot select retainer: 'RetainerList' window is not visible.");
            return false;
        }

        var elements = window.GetElements().ToList();
        var retainerRows = elements
            .Where(e => e.Type == NativeUiElementType.Button && e.Text.Contains(" | "))
            .ToList();

        var pattern = $@"(?:^|\|\s*){Regex.Escape(retainerName)}$";
        var targetRetainer = retainerRows.FirstOrDefault(e => Regex.IsMatch(e.Text, pattern, RegexOptions.IgnoreCase));

        if (targetRetainer == null) {
            this.logger.Warning($"Retainer '{retainerName}' not found in the active RetainerList.");
            return false;
        }

        var retainerIndex = retainerRows.IndexOf(targetRetainer);
        this.SelectRetainer(retainerIndex);
        return true;
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

        addon->FireCallback(2u, values, true);
    }

    public bool IsMenuReadyForRetainer(string retainerName) {
        var window = this.windowService.GetWindow("SelectString");
        if (window == null || !window.IsVisible) {
            return false;
        }

        return window.GetElements()
            .Where(e => e.Type == NativeUiElementType.Text)
            .Any(e => e.Text.Contains(retainerName, StringComparison.OrdinalIgnoreCase));
    }

    public bool IsMenuOptionAvailable(string optionText) {
        var window = this.windowService.GetWindow("SelectString");
        if (window == null || !window.IsVisible) {
            return false;
        }

        return window.GetElements()
            .Where(e => e.Type == NativeUiElementType.Button)
            .Any(e => e.Text.StartsWith(optionText, StringComparison.OrdinalIgnoreCase));
    }

    public bool SelectMenuOption(string optionText) {
        var window = this.windowService.GetWindow("SelectString");
        if (window == null || !window.IsVisible) {
            return false;
        }

        var elements = window.GetElements().ToList();
        var menuRows = elements.Where(e => e.Type == NativeUiElementType.Button).ToList();

        var targetOption = menuRows.FirstOrDefault(e => e.Text.StartsWith(optionText, StringComparison.OrdinalIgnoreCase));

        if (targetOption == null) {
            return false;
        }

        var optionIndex = menuRows.IndexOf(targetOption);
        window.SendCallbackWithUpdateState(true, optionIndex);
        return true;
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

        addon->FireCallback(2u, values, true);
    }

    public bool CloseRetainerMarket() {
        var addonPtr = this.gameGui.GetAddonByName("RetainerSellList");
        if (addonPtr.Address == IntPtr.Zero) {
            return false;
        }

        var addon = (AtkUnitBase*)addonPtr.Address;
        var values = stackalloc AtkValue[1];
        values[0].Type = AtkValueType.Int;
        values[0].Int = -1;

        addon->FireCallback(1u, values, true);
        return true;
    }

    public bool CloseSelectString() {
        var addonPtr = this.gameGui.GetAddonByName("SelectString");
        if (addonPtr.Address == IntPtr.Zero) {
            return false;
        }

        var addon = (AtkUnitBase*)addonPtr.Address;
        var values = stackalloc AtkValue[1];
        values[0].Type = AtkValueType.Int;
        values[0].Int = -1;

        addon->FireCallback(1u, values, true);
        return true;
    }

    public bool CloseSalesHistory() {
        var window = this.windowService.GetWindow("RetainerHistory");
        if (window == null || !window.IsVisible) {
            return false;
        }

        window.SendCallbackWithUpdateState(true, -1);
        return true;
    }

    public void SelectItemInSellList(int uiIndex) {
        var addonPtr = this.gameGui.GetAddonByName("RetainerSellList");
        if (addonPtr.Address == IntPtr.Zero) {
            return;
        }

        var addon = (AtkUnitBase*)addonPtr.Address;
        var values = stackalloc AtkValue[2];

        // Simulates a left click (Event 0) on the specified row (uiIndex) to open the ContextMenu
        values[0].Type = AtkValueType.Int;
        values[0].Int = 0;
        values[1].Type = AtkValueType.Int;
        values[1].Int = uiIndex;

        addon->FireCallback(2u, values, true);
    }

    public int GetContextMenuItemIndex(string localizedText) {
        var addonPtr = this.gameGui.GetAddonByName("ContextMenu");
        if (addonPtr.Address == IntPtr.Zero) {
            return -1;
        }

        var addon = (AtkUnitBase*)addonPtr.Address;
        if (!addon->IsVisible) {
            return -1;
        }

        var searchString = localizedText.Replace("’", "'");

        for (int i = 7; i < addon->AtkValuesCount; i++) {
            if (addon->AtkValues[i].Type == AtkValueType.String) {
                var ptr = (byte*)addon->AtkValues[i].String;
                if (ptr != null) {
                    var text = MemoryHelper.ReadSeStringNullTerminated((nint)ptr).TextValue;
                    if (text != null) {
                        var normalizedText = text.Replace("’", "'");
                        if (normalizedText.Contains(searchString, StringComparison.OrdinalIgnoreCase)) {
                            return i - 7;
                        }
                    }
                }
            }
        }
        return -1;
    }

    public void SelectContextMenuItem(int index) {
        var addonPtr = this.gameGui.GetAddonByName("ContextMenu");
        if (addonPtr.Address == IntPtr.Zero) {
            return;
        }

        var addon = (AtkUnitBase*)addonPtr.Address;
        var values = stackalloc AtkValue[5];
        values[0].Type = AtkValueType.Int;
        values[0].Int = 0;
        values[1].Type = AtkValueType.Int;
        values[1].Int = index;
        values[2].Type = AtkValueType.UInt;
        values[2].UInt = 0u;
        values[3].Type = AtkValueType.Int;
        values[3].Int = 0;
        values[4].Type = AtkValueType.Int;
        values[4].Int = 0;

        addon->FireCallback(5u, values, true);
    }

    public void ConfirmPriceUpdate(uint newPrice) {
        var addonPtr = this.gameGui.GetAddonByName("RetainerSell");
        if (addonPtr.Address == IntPtr.Zero) {
            return;
        }

        var addon = (AtkUnitBase*)addonPtr.Address;
        var values = stackalloc AtkValue[2];
        values[0].Type = AtkValueType.Int;
        values[0].Int = 0;
        values[1].Type = AtkValueType.UInt;
        values[1].UInt = newPrice;

        addon->FireCallback(2u, values, true);
    }

    public void ConfirmYesNo() {
        var addonPtr = this.gameGui.GetAddonByName("SelectYesNo");
        if (addonPtr.Address == IntPtr.Zero) {
            return;
        }

        var addon = (AtkUnitBase*)addonPtr.Address;
        var values = stackalloc AtkValue[1];
        values[0].Type = AtkValueType.Int;
        values[0].Int = 0;

        addon->FireCallback(1u, values, true);
    }

    public void CloseUnexpectedWindows() {
        this.ForceCloseAddon("ContextMenu");
        this.ForceCloseAddon("InputNumeric");
        this.ForceCloseAddon("SelectYesNo");
        this.ForceCloseAddon("RetainerSell");
    }

    private void ForceCloseAddon(string name) {
        var addonPtr = this.gameGui.GetAddonByName(name);
        if (addonPtr.Address == IntPtr.Zero) {
            return;
        }

        var addon = (AtkUnitBase*)addonPtr.Address;
        if (addon->IsVisible) {
            var values = stackalloc AtkValue[1];
            values[0].Type = AtkValueType.Int;
            values[0].Int = -1;
            addon->FireCallback(1u, values, true);
            addon->Close(true);
        }
    }

    public void SkipDialogue() {
        var addonPtr = this.gameGui.GetAddonByName("Talk");
        if (addonPtr.Address == IntPtr.Zero) {
            return;
        }

        var addon = (AtkUnitBase*)addonPtr.Address;
        if (addon->IsVisible) {
            var values = stackalloc AtkValue[1];
            values[0].Type = AtkValueType.Int;
            values[0].Int = 0;
            addon->FireCallback(1u, values, true);
        }
    }
}