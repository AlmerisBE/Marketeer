using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Marketeer.Core.Logging.Contracts;
using Marketeer.UI.UiInterop.Contracts;
using System;

namespace Marketeer.UI.UiInterop.Models;

public unsafe class NativeUiElement : INativeUiElement {
    private IGameGui gameGui;
    private ILoggerService logger;
    private string parentAddonName;

    public string Text { get; private set; }
    public NativeUiElementType Type { get; private set; }
    public uint NodeId { get; private set; }

    public NativeUiElement(
        string text,
        NativeUiElementType type,
        uint nodeId,
        string parentAddonName,
        IGameGui gameGui,
        ILoggerService logger) {

        this.Text = text;
        this.Type = type;
        this.NodeId = nodeId;
        this.parentAddonName = parentAddonName;
        this.gameGui = gameGui;
        this.logger = logger;
    }

    public void Click() {
        if (this.Type != NativeUiElementType.Button && this.Type != NativeUiElementType.Link) {
            this.logger.Warning($"[NativeUiElement] Attempted to click a non-clickable element: {this.Text} (ID: {this.NodeId})");
            return;
        }

        var addonPtr = this.gameGui.GetAddonByName(this.parentAddonName);
        if (addonPtr.Address == IntPtr.Zero) {
            this.logger.Error($"[NativeUiElement] Parent addon '{this.parentAddonName}' not found for click action.");
            return;
        }

        var addon = (AtkUnitBase*)addonPtr.Address;

        // Note: Generic clicks via memory require firing specific callbacks. 
        // We simulate a generic interaction here by passing the NodeId, 
        // though specific addons might require targeted event routing.
        var values = stackalloc AtkValue[1];
        values[0].Type = AtkValueType.UInt;
        values[0].UInt = this.NodeId;

        this.logger.Debug($"[NativeUiElement] Firing generic click callback for NodeId {this.NodeId} on '{this.parentAddonName}'.");
        addon->FireCallback(1, values);
    }
}