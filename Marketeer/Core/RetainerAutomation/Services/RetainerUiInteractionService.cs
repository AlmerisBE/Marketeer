using Dalamud.Memory;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Marketeer.Core.Logging.Contracts;
using Marketeer.Core.RetainerAutomation.Contracts;
using Marketeer.UI.UiInterop.Contracts;
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
        if (addonPtr.Address == IntPtr.Zero) return false;

        var addon = (AtkUnitBase*)addonPtr.Address;
        return addon->IsVisible && addon->UldManager.LoadedState == AtkLoadState.Loaded;
    }

    public bool IsRetainerAvailable(string retainerName) {
        if (!this.IsAddonReady("RetainerList")) return false;

        var window = this.windowService.GetWindow("RetainerList");
        if (window == null || !window.IsVisible) return false;

        var pattern = $@"(?:^|\|\s*){Regex.Escape(retainerName)}$";
        return window.GetElements()
            .Where(e => e.Type == NativeUiElementType.Button && e.Text.Contains(" | "))
            .Any(e => Regex.IsMatch(e.Text, pattern, RegexOptions.IgnoreCase));
    }

    public bool SelectRetainer(string retainerName) {
        if (!this.IsAddonReady("RetainerList")) return false;

        var window = this.windowService.GetWindow("RetainerList");
        if (window == null || !window.IsVisible) return false;

        var elements = window.GetElements().ToList();
        var retainerRows = elements
            .Where(e => e.Type == NativeUiElementType.Button && e.Text.Contains(" | "))
            .ToList();

        var pattern = $@"(?:^|\|\s*){Regex.Escape(retainerName)}$";
        var targetRetainer = retainerRows.FirstOrDefault(e => Regex.IsMatch(e.Text, pattern, RegexOptions.IgnoreCase));

        if (targetRetainer == null) return false;

        var retainerIndex = retainerRows.IndexOf(targetRetainer);
        this.SelectRetainer(retainerIndex);
        return true;
    }

    public void SelectRetainer(int index) {
        var addonPtr = this.gameGui.GetAddonByName("RetainerList");
        if (addonPtr.Address == IntPtr.Zero) return;

        var addon = (AtkUnitBase*)addonPtr.Address;
        var values = stackalloc AtkValue[2];
        values[0].Type = AtkValueType.Int;
        values[0].Int = 2;
        values[1].Type = AtkValueType.Int;
        values[1].Int = index;

        addon->FireCallback(2u, values, true);
    }

    public bool IsMenuReadyForRetainer(string retainerName) {
        if (!this.IsAddonReady("SelectString")) return false;

        var window = this.windowService.GetWindow("SelectString");
        if (window == null || !window.IsVisible) return false;

        return window.GetElements()
            .Where(e => e.Type == NativeUiElementType.Text)
            .Any(e => e.Text.Contains(retainerName, StringComparison.OrdinalIgnoreCase));
    }

    public bool IsMenuOptionAvailable(string optionText) {
        if (!this.IsAddonReady("SelectString")) return false;

        var window = this.windowService.GetWindow("SelectString");
        if (window == null || !window.IsVisible) return false;

        return window.GetElements()
            .Where(e => e.Type == NativeUiElementType.Button)
            .Any(e => e.Text.StartsWith(optionText, StringComparison.OrdinalIgnoreCase));
    }

    public bool SelectMenuOption(string optionText) {
        if (!this.IsAddonReady("SelectString")) return false;

        var window = this.windowService.GetWindow("SelectString");
        if (window == null || !window.IsVisible) return false;

        var elements = window.GetElements().ToList();
        var menuRows = elements.Where(e => e.Type == NativeUiElementType.Button).ToList();

        var targetOption = menuRows.FirstOrDefault(e => e.Text.StartsWith(optionText, StringComparison.OrdinalIgnoreCase));
        if (targetOption == null) return false;

        var optionIndex = menuRows.IndexOf(targetOption);
        window.SendCallbackWithUpdateState(true, optionIndex);
        return true;
    }

    public void OpenRetainerMarket() {
        var addonPtr = this.gameGui.GetAddonByName("SelectString");
        if (addonPtr.Address == IntPtr.Zero) return;

        var addon = (AtkUnitBase*)addonPtr.Address;
        var values = stackalloc AtkValue[2];
        values[0].Type = AtkValueType.Int;
        values[0].Int = 2;

        addon->FireCallback(2u, values, true);
    }

    public bool CloseRetainerMarket() {
        var addonPtr = this.gameGui.GetAddonByName("RetainerSellList");
        if (addonPtr.Address == IntPtr.Zero) return true;

        var addon = (AtkUnitBase*)addonPtr.Address;
        if (!addon->IsVisible) return true;

        var values = stackalloc AtkValue[1];
        values[0].Type = AtkValueType.Int;
        values[0].Int = -1;

        addon->FireCallback(1u, values, true);
        return true;
    }

    public bool CloseSelectString() {
        var addonPtr = this.gameGui.GetAddonByName("SelectString");
        if (addonPtr.Address == IntPtr.Zero) return true;

        var addon = (AtkUnitBase*)addonPtr.Address;
        if (!addon->IsVisible) return true;

        var values = stackalloc AtkValue[1];
        values[0].Type = AtkValueType.Int;
        values[0].Int = -1;

        addon->FireCallback(1u, values, true);
        return true;
    }

    public bool CloseSalesHistory() {
        var window = this.windowService.GetWindow("RetainerHistory");
        if (window == null || !window.IsVisible) return true;

        window.SendCallbackWithUpdateState(true, -1);
        return true;
    }

    private unsafe void SendNativeClick(nint listenerAddr, int eventType, uint eventParam, void* targetNode) {
        if (listenerAddr == IntPtr.Zero || targetNode == null) {
            this.logger.Warning($"[SendNativeClick] Aborted due to null pointer. Listener: {listenerAddr:X}, Target: {(nint)targetNode:X}");
            return;
        }

        var listener = (AtkEventListener*)listenerAddr;

        // Allocation native conforme à la structure AtkEvent de FFXIV
        var eventData = System.Runtime.InteropServices.Marshal.AllocHGlobal(0x40);
        for (var i = 0; i < 0x40; i++) System.Runtime.InteropServices.Marshal.WriteByte(eventData, i, 0);

        // Offset 0x08: Target AtkResNode/AtkComponentNode
        System.Runtime.InteropServices.Marshal.WriteIntPtr(eventData, 0x8, new IntPtr(targetNode));
        // Offset 0x10: Listener/Owner Addon Pointer (AtkUnitBase)
        System.Runtime.InteropServices.Marshal.WriteIntPtr(eventData, 0x10, listenerAddr);

        var eventParamData = System.Runtime.InteropServices.Marshal.AllocHGlobal(0x40);
        for (var i = 0; i < 0x40; i++) System.Runtime.InteropServices.Marshal.WriteByte(eventParamData, i, 0);

        listener->ReceiveEvent((AtkEventType)eventType, (int)eventParam, (AtkEvent*)eventData, (AtkEventData*)eventParamData);

        System.Runtime.InteropServices.Marshal.FreeHGlobal(eventData);
        System.Runtime.InteropServices.Marshal.FreeHGlobal(eventParamData);
    }

    public unsafe void OpenComparePrices(nint addonAddress = 0) {
        nint targetAddonAddr = addonAddress;
        if (targetAddonAddr == IntPtr.Zero) {
            var addonPtr = this.gameGui.GetAddonByName("RetainerSell");
            targetAddonAddr = addonPtr.Address;
        }

        if (targetAddonAddr == IntPtr.Zero) return;

        var addon = (AtkUnitBase*)targetAddonAddr;
        if (!addon->IsVisible) return;

        // FFXIV RetainerSell callback signature for "Compare Prices" button is Event ID 1 (or 4 depending on internal index)
        // FireCallback directly triggers the native internal handler without manual AtkEvent marshaling
        var values = stackalloc AtkValue[2];
        values[0].Type = AtkValueType.Int;
        values[0].Int = 4; // Action ID 4 corresponds to Compare Prices button callback
        values[1].Type = AtkValueType.Int;
        values[1].Int = 0;

        this.logger.Debug($"[RetainerUiInteractionService] FireCallback(4) dispatched to RetainerSell at {targetAddonAddr:X}");
        addon->FireCallback(2u, values, true);

        // Fallback: If FireCallback(4) is ignored by specific game versions, try FireCallback(1) which handles default sub-actions
        var fallbackValues = stackalloc AtkValue[1];
        fallbackValues[0].Type = AtkValueType.Int;
        fallbackValues[0].Int = 1;
        addon->FireCallback(1u, fallbackValues, true);
    }

    public unsafe void SetPriceAndConfirm(uint newPrice) {
        var addonPtr = this.gameGui.GetAddonByName("RetainerSell");
        if (addonPtr.Address == IntPtr.Zero) return;

        var addon = (FFXIVClientStructs.FFXIV.Client.UI.AddonRetainerSell*)addonPtr.Address;

        if (addon->AtkUnitBase.UldManager.NodeListCount > 15) {
            var priceNode = addon->AtkUnitBase.UldManager.NodeList[15];
            if (priceNode != null && (ushort)priceNode->Type >= 1000) {
                var compNode = (AtkComponentNode*)priceNode;
                var numericInput = (AtkComponentNumericInput*)compNode->Component;
                if (numericInput != null) {
                    numericInput->SetValue((int)newPrice);
                }
            }
        }

        var values = stackalloc AtkValue[2];
        values[0].Type = AtkValueType.Int; values[0].Int = 0;
        values[1].Type = AtkValueType.UInt; values[1].UInt = newPrice;
        addon->AtkUnitBase.FireCallback(2u, values, true);

        // Corrected: Passing the Confirm button's OwnerNode instead of the Component
        if (addon->Confirm != null && addon->Confirm->AtkComponentBase.OwnerNode != null) {
            this.SendNativeClick(addonPtr.Address, 2, 21, addon->Confirm->AtkComponentBase.OwnerNode);
        }
    }

    public unsafe void ConfirmPriceUpdate(uint newPrice) {
        var addonPtr = this.gameGui.GetAddonByName("RetainerSell");
        if (addonPtr.Address == IntPtr.Zero) return;

        var addon = (FFXIVClientStructs.FFXIV.Client.UI.AddonRetainerSell*)addonPtr.Address;

        var values = stackalloc AtkValue[2];
        values[0].Type = AtkValueType.Int;
        values[0].Int = 0;
        values[1].Type = AtkValueType.UInt;
        values[1].UInt = newPrice;

        addon->AtkUnitBase.FireCallback(2u, values, true);

        // Corrected: Ensure bulk updates also click the OwnerNode properly
        if (addon->Confirm != null && addon->Confirm->AtkComponentBase.OwnerNode != null) {
            this.SendNativeClick(addonPtr.Address, 2, 21, addon->Confirm->AtkComponentBase.OwnerNode);
        }
    }

    public unsafe void CloseItemSearchResult() {
        var addonPtr = this.gameGui.GetAddonByName("ItemSearchResult");
        if (addonPtr.Address == IntPtr.Zero) return;

        var addon = (AtkUnitBase*)addonPtr.Address;
        if (!addon->IsVisible) return;

        var values = stackalloc AtkValue[1];
        values[0].Type = AtkValueType.Int; values[0].Int = -1;
        addon->FireCallback(1u, values, true);
    }

    public void SelectItemInSellList(int uiIndex) {
        var addonPtr = this.gameGui.GetAddonByName("RetainerSellList");
        if (addonPtr.Address == IntPtr.Zero) return;

        var addon = (AtkUnitBase*)addonPtr.Address;
        var values = stackalloc AtkValue[3];

        values[0].Type = AtkValueType.Int;
        values[0].Int = 0;
        values[1].Type = AtkValueType.Int;
        values[1].Int = uiIndex;
        values[2].Type = AtkValueType.Int;
        values[2].Int = 0;

        this.logger.Debug($"[RetainerUiInteractionService] Firing Event ID 0 on RetainerSellList at UI index {uiIndex}.");
        addon->FireCallback(3u, values, true);
    }

    public int GetContextMenuItemIndex(string localizedText) {
        var addonPtr = this.gameGui.GetAddonByName("ContextMenu");
        if (addonPtr.Address == IntPtr.Zero) return -1;

        var addon = (AtkUnitBase*)addonPtr.Address;
        if (!addon->IsVisible) return -1;

        var searchString = localizedText.Replace("'", "").Replace("’", "").ToLowerInvariant();

        for (int i = 7; i < addon->AtkValuesCount; i++) {
            if (addon->AtkValues[i].Type == AtkValueType.String) {
                var ptr = (byte*)addon->AtkValues[i].String;
                if (ptr != null) {
                    var text = MemoryHelper.ReadSeStringNullTerminated((nint)ptr).TextValue;
                    if (text != null) {
                        var normalizedText = text.Replace("'", "").Replace("’", "").ToLowerInvariant();
                        if (normalizedText.Contains(searchString)) return i - 7;
                    }
                }
            }
        }
        return -1;
    }

    public void SelectContextMenuItem(int index) {
        var addonPtr = this.gameGui.GetAddonByName("ContextMenu");
        if (addonPtr.Address == IntPtr.Zero) return;

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

    public void ConfirmYesNo() {
        var addonPtr = this.gameGui.GetAddonByName("SelectYesNo");
        if (addonPtr.Address == IntPtr.Zero) return;

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
        this.ForceCloseAddon("ItemSearchResult");
        this.ForceCloseAddon("RetainerSell");
    }

    private void ForceCloseAddon(string name) {
        var addonPtr = this.gameGui.GetAddonByName(name);
        if (addonPtr.Address == IntPtr.Zero) return;

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
        if (addonPtr.Address == IntPtr.Zero) return;

        var addon = (AtkUnitBase*)addonPtr.Address;
        if (addon->IsVisible) {
            var values = stackalloc AtkValue[1];
            values[0].Type = AtkValueType.Int;
            values[0].Int = 0;
            addon->FireCallback(1u, values, true);
        }
    }
}