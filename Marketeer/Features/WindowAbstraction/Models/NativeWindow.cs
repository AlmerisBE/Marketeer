using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Marketeer.Features.Logging.Contracts;
using Marketeer.Features.WindowAbstraction.Contracts;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Marketeer.Features.WindowAbstraction.Models;

public unsafe class NativeWindow : INativeWindow {
    private IGameGui gameGui;
    private INativeWindowService windowService;
    private IWindowHierarchyProvider hierarchyProvider;
    private ILoggerService logger;

    public string Name { get; private set; }
    public WindowType Type { get; private set; }

    public bool IsVisible {
        get {
            var addon = this.GetAtkUnitBase();
            return addon != null && addon->IsVisible;
        }
    }

    public INativeWindow? Parent {
        get {
            var parentName = this.hierarchyProvider.GetParentWindowName(this.Name);
            if (string.IsNullOrEmpty(parentName)) {
                return null;
            }
            return this.windowService.GetWindow(parentName);
        }
    }

    public NativeWindow(
        string name,
        WindowType type,
        IGameGui gameGui,
        INativeWindowService windowService,
        IWindowHierarchyProvider hierarchyProvider,
        ILoggerService logger) {

        this.Name = name;
        this.Type = type;
        this.gameGui = gameGui;
        this.windowService = windowService;
        this.hierarchyProvider = hierarchyProvider;
        this.logger = logger;
    }

    public void Close(bool closeHierarchy = true) {
        if (closeHierarchy && this.Parent != null && this.Parent.IsVisible) {
            this.Parent.Close(true);
        }

        var addon = this.GetAtkUnitBase();
        if (addon == null || !addon->IsVisible) {
            return;
        }

        if (this.Type == WindowType.Dialog || this.Type == WindowType.Menu) {
            var values = stackalloc AtkValue[1];
            values[0].Type = AtkValueType.Int;
            values[0].Int = -1;
            addon->FireCallback(1u, values);
        }
        else {
            addon->Close(true);
        }
    }

    public void SendCallback(params object[] args) {
        var addon = this.GetAtkUnitBase();

        if (addon == null || !addon->IsVisible) {
            this.logger.Warning($"[NativeWindow] Cannot send callback to '{this.Name}': window is not visible.");
            return;
        }

        if (args == null || args.Length == 0) {
            return;
        }

        var values = stackalloc AtkValue[args.Length];

        for (int i = 0; i < args.Length; i++) {
            if (args[i] is int intValue) {
                values[i].Type = AtkValueType.Int;
                values[i].Int = intValue;
            }
            else if (args[i] is uint uintValue) {
                values[i].Type = AtkValueType.UInt;
                values[i].UInt = uintValue;
            }
            else if (args[i] is bool boolValue) {
                values[i].Type = AtkValueType.Int;
                values[i].Int = boolValue ? 1 : 0;
            }
            else {
                this.logger.Warning($"[NativeWindow] Unsupported argument type '{args[i].GetType().Name}' at index {i} for callback.");
            }
        }

        this.logger.Info($"[NativeWindow] Sending callback to '{this.Name}' with {args.Length} arguments (Event ID: {args[0]}).");
        addon->FireCallback((uint)args.Length, values);
    }

    public IEnumerable<INativeUiElement> GetElements() {
        var addon = this.GetAtkUnitBase();
        var elements = new List<INativeUiElement>();

        if (addon == null || !addon->IsVisible) {
            return elements;
        }

        this.ExtractElementsRecursively(&addon->UldManager, elements, false);
        return elements;
    }

    private void ExtractElementsRecursively(AtkUldManager* uldManager, List<INativeUiElement> elements, bool ignoreRawText) {
        if (uldManager == null) {
            return;
        }

        // Iterate forward to match the game's internal data structures naturally.
        for (int i = 0; i < uldManager->NodeListCount; i++) {
            var node = uldManager->NodeList[i];
            if (node == null) {
                continue;
            }

            if (node->Type == NodeType.Text) {
                if (!ignoreRawText) {
                    var textNode = (AtkTextNode*)node;
                    var text = this.ExtractString(textNode->NodeText.StringPtr);

                    if (!string.IsNullOrWhiteSpace(text)) {
                        elements.Add(new NativeUiElement(text, NativeUiElementType.Text, node->NodeId, this.Name, this.gameGui, this.logger));
                    }
                }
            }
            else if ((ushort)node->Type >= 1000) {
                var compNode = (AtkComponentNode*)node;
                var comp = compNode->Component;

                if (comp != null) {
                    var aggregatedText = this.GetAggregatedTextDirect(&comp->UldManager);
                    bool isButton = !string.IsNullOrWhiteSpace(aggregatedText);

                    if (isButton) {
                        elements.Add(new NativeUiElement(aggregatedText, NativeUiElementType.Button, node->NodeId, this.Name, this.gameGui, this.logger));
                    }

                    this.ExtractElementsRecursively(&comp->UldManager, elements, ignoreRawText || isButton);
                }
            }
        }
    }

    private string GetAggregatedTextDirect(AtkUldManager* uldManager) {
        if (uldManager == null) {
            return string.Empty;
        }

        var sb = new StringBuilder();

        // Iterate forward to match the game's internal data structures naturally.
        for (int i = 0; i < uldManager->NodeListCount; i++) {
            var node = uldManager->NodeList[i];
            if (node != null && node->Type == NodeType.Text) {
                var textNode = (AtkTextNode*)node;
                var text = this.ExtractString(textNode->NodeText.StringPtr);

                if (!string.IsNullOrWhiteSpace(text)) {
                    sb.Append(text).Append(" | ");
                }
            }
        }

        var result = sb.ToString();
        return result.Length > 0 ? result.TrimEnd(' ', '|') : string.Empty;
    }

    private string ExtractString(byte* stringPtr) {
        if (stringPtr == null) {
            return string.Empty;
        }
        return Marshal.PtrToStringUTF8((IntPtr)stringPtr) ?? string.Empty;
    }

    private AtkUnitBase* GetAtkUnitBase() {
        var addonPtr = this.gameGui.GetAddonByName(this.Name);
        if (addonPtr.Address == IntPtr.Zero) {
            return null;
        }
        return (AtkUnitBase*)addonPtr.Address;
    }
}