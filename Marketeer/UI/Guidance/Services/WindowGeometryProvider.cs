using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Marketeer.UI.Guidance.Contracts;
using System;

namespace Marketeer.UI.Guidance.Services;

public unsafe class WindowGeometryProvider : IWindowGeometryProvider {
    private IGameGui gameGui;

    public WindowGeometryProvider(IGameGui gameGui) {
        this.gameGui = gameGui;
    }

    public bool GetWindowGeometry(string windowName, out float x, out float y, out float width, out float height, out float scale) {
        x = 0;
        y = 0;
        width = 0;
        height = 0;
        scale = 1f;

        var addonPtr = this.gameGui.GetAddonByName(windowName);
        if (addonPtr.Address == IntPtr.Zero) {
            return false;
        }

        var addon = (AtkUnitBase*)addonPtr.Address;
        if (!addon->IsVisible) {
            return false;
        }

        scale = addon->Scale;
        x = addon->X;
        y = addon->Y;

        if (addon->RootNode != null) {
            width = addon->RootNode->Width;
            height = addon->RootNode->Height;
        }

        return true;
    }
}