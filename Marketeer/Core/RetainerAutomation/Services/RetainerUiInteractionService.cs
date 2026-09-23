using Dalamud.Memory;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Marketeer.API.UiInterop.Contracts;
using Marketeer.Core.Logging.Contracts;
using Marketeer.Core.RetainerAutomation.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
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

        return addon->IsVisible && addon->UldManager.NodeListCount > 0;
    }

    public bool IsRetainerAvailable(string retainerName) {
        if (!this.IsAddonReady("RetainerList")) return false;

        var manager = FFXIVClientStructs.FFXIV.Client.Game.RetainerManager.Instance();
        if (manager == null) return false;

        for (uint i = 0; i < 10; i++) {
            var retainer = manager->GetRetainerBySortedIndex(i);

            if (retainer != null && retainer->RetainerId != 0) {
                var nameSpan = retainer->Name;
                var nullIndex = nameSpan.IndexOf((byte)0);
                var actualSpan = nullIndex >= 0 ? nameSpan[..nullIndex] : nameSpan;
                var name = Encoding.UTF8.GetString(actualSpan);

                if (name.Equals(retainerName, StringComparison.OrdinalIgnoreCase)) return true;
            }
        }

        return false;
    }

    public bool SelectRetainer(string retainerName) {
        if (!this.IsAddonReady("RetainerList")) return false;

        var manager = FFXIVClientStructs.FFXIV.Client.Game.RetainerManager.Instance();
        if (manager == null) return false;

        for (uint i = 0; i < 10; i++) {
            var retainer = manager->GetRetainerBySortedIndex(i);

            if (retainer != null && retainer->RetainerId != 0) {
                var nameSpan = retainer->Name;
                var nullIndex = nameSpan.IndexOf((byte)0);
                var actualSpan = nullIndex >= 0 ? nameSpan[..nullIndex] : nameSpan;
                var name = Encoding.UTF8.GetString(actualSpan);

                if (name.Equals(retainerName, StringComparison.OrdinalIgnoreCase)) {
                    this.SelectRetainer((int)i);
                    return true;
                }
            }
        }

        return false;
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

        var eventData = Marshal.AllocHGlobal(0x40);
        for (var i = 0; i < 0x40; i++) Marshal.WriteByte(eventData, i, 0);

        Marshal.WriteIntPtr(eventData, 0x8, new IntPtr(targetNode));
        Marshal.WriteIntPtr(eventData, 0x10, listenerAddr);

        var eventParamData = Marshal.AllocHGlobal(0x40);
        for (var i = 0; i < 0x40; i++) Marshal.WriteByte(eventParamData, i, 0);

        listener->ReceiveEvent((AtkEventType)eventType, (int)eventParam, (AtkEvent*)eventData, (AtkEventData*)eventParamData);

        Marshal.FreeHGlobal(eventData);
        Marshal.FreeHGlobal(eventParamData);
    }

    public unsafe bool GetActiveRetainerSellItemData(out List<string> windowTexts, out uint currentPrice) {
        windowTexts = new List<string>();
        currentPrice = 0;

        var addonPtr = this.gameGui.GetAddonByName("RetainerSell");
        if (addonPtr.Address == IntPtr.Zero) return false;

        var addon = (AtkUnitBase*)addonPtr.Address;
        if (!addon->IsVisible) return false;

        for (int i = 0; i < addon->UldManager.NodeListCount; i++) {
            var node = addon->UldManager.NodeList[i];
            if (node != null && node->Type == NodeType.Text && node->IsVisible()) {
                var textNode = (AtkTextNode*)node;
                var ptr = (byte*)textNode->NodeText.StringPtr;

                if (ptr != null) {
                    var text = MemoryHelper.ReadSeStringNullTerminated((nint)ptr).TextValue;
                    if (!string.IsNullOrWhiteSpace(text)) {
                        var cleanText = Regex.Replace(text.Replace("\uE03C", "").Replace("", ""), @"\s+", " ").Trim();
                        windowTexts.Add(cleanText);
                    }
                }
            }
        }

        if (addon->UldManager.NodeListCount > 15) {
            var priceNode = addon->UldManager.NodeList[15];
            if (priceNode != null && (ushort)priceNode->Type >= 1000) {
                var compNode = (AtkComponentNode*)priceNode;
                var numericInput = (AtkComponentNumericInput*)compNode->Component;
                if (numericInput != null) currentPrice = (uint)numericInput->Value;
            }
        }

        return windowTexts.Count > 0;
    }

    public unsafe void OpenComparePrices(nint addonAddress = 0) {
        nint targetAddonAddr = addonAddress == IntPtr.Zero ? this.gameGui.GetAddonByName("RetainerSell").Address : addonAddress;
        if (targetAddonAddr == IntPtr.Zero) return;

        var addon = (AtkUnitBase*)targetAddonAddr;
        if (!addon->IsVisible) return;

        var values = stackalloc AtkValue[2];
        values[0].Type = AtkValueType.Int;
        values[0].Int = 4;
        values[1].Type = AtkValueType.Int;
        values[1].Int = 0;

        addon->FireCallback(2u, values, true);
    }

    public unsafe void SetPriceAndConfirm(uint newPrice) {
        var addonPtr = this.gameGui.GetAddonByName("RetainerSell");
        if (addonPtr.Address == IntPtr.Zero) return;

        var addon = (AddonRetainerSell*)addonPtr.Address;

        if (addon->AtkUnitBase.UldManager.NodeListCount > 15) {
            var priceNode = addon->AtkUnitBase.UldManager.NodeList[15];
            if (priceNode != null && (ushort)priceNode->Type >= 1000) {
                var compNode = (AtkComponentNode*)priceNode;
                var numericInput = (AtkComponentNumericInput*)compNode->Component;
                if (numericInput != null) numericInput->SetValue((int)newPrice);
            }
        }

        var values = stackalloc AtkValue[2];
        values[0].Type = AtkValueType.Int;
        values[0].Int = 0;
        values[1].Type = AtkValueType.UInt;
        values[1].UInt = newPrice;
        addon->AtkUnitBase.FireCallback(2u, values, true);

        if (addon->Confirm != null && addon->Confirm->AtkComponentBase.OwnerNode != null) {
            this.SendNativeClick(addonPtr.Address, 2, 21, addon->Confirm->AtkComponentBase.OwnerNode);
        }
        else {
            var confirmValues = stackalloc AtkValue[1];
            confirmValues[0].Type = AtkValueType.Int;
            confirmValues[0].Int = 0;
            addon->AtkUnitBase.FireCallback(1u, confirmValues, true);
        }
    }

    public unsafe void ConfirmPriceUpdate(uint newPrice) {
        this.SetPriceAndConfirm(newPrice);
    }

    public unsafe void CloseItemSearchResult() {
        var addonPtr = this.gameGui.GetAddonByName("ItemSearchResult");
        if (addonPtr.Address == IntPtr.Zero) return;

        var addon = (AtkUnitBase*)addonPtr.Address;
        if (!addon->IsVisible) return;

        var values = stackalloc AtkValue[1];
        values[0].Type = AtkValueType.Int;
        values[0].Int = -1;
        addon->FireCallback(1u, values, true);

        addon->Close(true);
    }

    public unsafe void OpenContextMenuForSellList(int uiIndex) {
        var addonPtr = this.gameGui.GetAddonByName("RetainerSellList");
        if (addonPtr.Address == IntPtr.Zero) return;

        var addon = (AtkUnitBase*)addonPtr.Address;
        var values = stackalloc AtkValue[3];

        values[0].Type = AtkValueType.Int;
        values[0].Int = 1;
        values[1].Type = AtkValueType.Int;
        values[1].Int = uiIndex;
        values[2].Type = AtkValueType.Int;
        values[2].Int = 0;

        this.logger.Debug($"[RetainerUiInteractionService] Firing Event ID 1 (ContextMenu) on RetainerSellList at UI index {uiIndex}.");
        addon->FireCallback(3u, values, true);
    }

    public int GetContextMenuItemIndex(string localizedText) {
        return this.GetContextMenuItemIndex(new[] { localizedText });
    }

    public int GetContextMenuItemIndex(IEnumerable<string> localizedTexts) {
        var addonPtr = this.gameGui.GetAddonByName("ContextMenu");
        if (addonPtr.Address == IntPtr.Zero) return -1;

        var addon = (AtkUnitBase*)addonPtr.Address;
        var normalizedSearches = localizedTexts.Select(t => t.Replace("'", "").Replace("’", "").ToLowerInvariant().Trim()).ToList();

        for (int i = 8; i < addon->AtkValuesCount; i++) {
            var type = (int)addon->AtkValues[i].Type;

            if (type == 4 || type == 6 || type == 8 || type == 38 || type == 40) {
                var ptr = (byte*)addon->AtkValues[i].String;
                if (ptr != null) {
                    var text = MemoryHelper.ReadSeStringNullTerminated((nint)ptr).TextValue;
                    if (text != null) {
                        var normalizedText = text.Replace("'", "").Replace("’", "").ToLowerInvariant().Trim();
                        if (normalizedSearches.Any(s => normalizedText.Contains(s))) return i - 8;
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

    public unsafe string? GetRetainerSellListItemName(int index) {
        var addonPtr = this.gameGui.GetAddonByName("RetainerSellList");
        if (addonPtr.Address == IntPtr.Zero) return null;

        var addon = (AtkUnitBase*)addonPtr.Address;
        if (!addon->IsVisible) return null;

        var listNode = addon->UldManager.SearchNodeById(11);
        if (listNode == null || listNode->Type != NodeType.Component) return null;

        var listComponent = (AtkComponentList*)((AtkComponentNode*)listNode)->Component;
        if (listComponent == null) return null;

        if (index < 0 || index >= listComponent->ListLength) return null;

        var renderer = listComponent->ItemRendererList[index].AtkComponentListItemRenderer;
        if (renderer == null) return null;

        var textNode = (AtkTextNode*)renderer->AtkComponentButton.AtkComponentBase.UldManager.SearchNodeById(3);
        if (textNode == null) return null;

        var ptr = (byte*)textNode->NodeText.StringPtr;
        if (ptr == null) return null;

        var text = MemoryHelper.ReadSeStringNullTerminated((nint)ptr).TextValue;
        if (string.IsNullOrWhiteSpace(text)) return null;

        return Regex.Replace(text.Replace("\uE03C", "").Replace("", ""), @"\s+", " ").Trim();
    }
}