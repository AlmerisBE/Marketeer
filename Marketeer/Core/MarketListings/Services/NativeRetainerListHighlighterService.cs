using Dalamud.Bindings.ImGui;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Memory;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Graphics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Marketeer.API.CompetitionTracking.Contracts;
using Marketeer.API.Logging.Contracts;
using Marketeer.API.MarketListings.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Marketeer.Core.MarketListings.Services;

public class NativeRetainerListHighlighterService : IDisposable {
    private struct OverlayTextData {
        public Vector2 Position;
        public uint Color;
        public string Text;
    }

    private IAddonLifecycle addonLifecycle;
    private ICompetitionStateService competitionState;
    private IListingOptimizationService optimizationService;
    private IObjectTable objectTable;
    private ILoggerService logger;
    private IDalamudPluginInterface pluginInterface;
    private IGameGui gameGui;

    private List<OverlayTextData> overlayData = new();

    public NativeRetainerListHighlighterService(
        IAddonLifecycle addonLifecycle,
        ICompetitionStateService competitionState,
        IListingOptimizationService optimizationService,
        IObjectTable objectTable,
        ILoggerService logger,
        IDalamudPluginInterface pluginInterface,
        IGameGui gameGui) {

        this.addonLifecycle = addonLifecycle;
        this.competitionState = competitionState;
        this.optimizationService = optimizationService;
        this.objectTable = objectTable;
        this.logger = logger;
        this.pluginInterface = pluginInterface;
        this.gameGui = gameGui;

        this.addonLifecycle.RegisterListener(AddonEvent.PostUpdate, "RetainerList", this.OnRetainerListUpdate);
        this.pluginInterface.UiBuilder.Draw += this.OnDrawOverlay;
    }

    private unsafe void OnRetainerListUpdate(AddonEvent type, AddonArgs args) {
        try {
            this.overlayData.Clear();

            var addon = (AtkUnitBase*)args.Addon.Address;
            if (addon == null || !addon->IsVisible) {
                return;
            }

            var localPlayer = this.objectTable.LocalPlayer;
            if (localPlayer == null) {
                return;
            }

            var playerName = localPlayer.Name.TextValue;

            var undercuts = this.competitionState.GetUndercutItems().Where(u => u.CharacterName == playerName);
            var suboptimals = this.optimizationService.GetVendorPricedListings().Where(s => s.CharacterName == playerName);

            var retainerActions = new Dictionary<string, (int Undercuts, int Suboptimals)>();

            foreach (var u in undercuts) {
                if (!retainerActions.ContainsKey(u.RetainerName)) {
                    retainerActions[u.RetainerName] = (0, 0);
                }

                retainerActions[u.RetainerName] = (retainerActions[u.RetainerName].Undercuts + 1, retainerActions[u.RetainerName].Suboptimals);
            }

            foreach (var s in suboptimals) {
                if (!retainerActions.ContainsKey(s.RetainerName)) {
                    retainerActions[s.RetainerName] = (0, 0);
                }

                retainerActions[s.RetainerName] = (retainerActions[s.RetainerName].Undercuts, retainerActions[s.RetainerName].Suboptimals + 1);
            }

            if (retainerActions.Count == 0) {
                return;
            }

            this.TraverseAndHighlight(addon, &addon->UldManager, retainerActions);
        }
        catch (Exception ex) {
            this.logger.Error(ex, "Failed to apply native highlights to RetainerList.");
        }
    }

    private unsafe void TraverseAndHighlight(AtkUnitBase* addon, AtkUldManager* uldManager, Dictionary<string, (int Undercuts, int Suboptimals)> actions) {
        if (uldManager == null) {
            return;
        }

        for (int i = 0; i < uldManager->NodeListCount; i++) {
            var node = uldManager->NodeList[i];
            if (node == null || !node->IsVisible()) {
                continue;
            }

            if ((ushort)node->Type >= 1000) {
                var compNode = (AtkComponentNode*)node;
                var comp = compNode->Component;
                if (comp != null) {
                    var textNodes = new List<nint>();
                    this.CollectVisibleTextNodes(&comp->UldManager, textNodes);

                    if (textNodes.Count >= 5) {
                        textNodes = textNodes.OrderBy(this.GetAbsoluteX).ToList();

                        var nameNode = (AtkTextNode*)textNodes[0];
                        var cleanNameText = this.ExtractString(nameNode->NodeText.StringPtr).Trim();

                        string? matchedRetainer = null;

                        foreach (var retainerName in actions.Keys.OrderByDescending(k => k.Length)) {
                            if (cleanNameText.StartsWith(retainerName, StringComparison.InvariantCultureIgnoreCase)) {
                                matchedRetainer = retainerName;
                                break;
                            }
                        }

                        if (matchedRetainer != null) {
                            var counts = actions[matchedRetainer];

                            ByteColor targetColor;
                            uint imGuiColor;

                            if (counts.Suboptimals > 0 && counts.Undercuts > 0) {
                                targetColor = new ByteColor { A = 255, R = 255, G = 150, B = 50 };
                                imGuiColor = ImGui.GetColorU32(new Vector4(1f, 0.59f, 0.2f, 1f));
                            }
                            else if (counts.Suboptimals > 0) {
                                targetColor = new ByteColor { A = 255, R = 255, G = 60, B = 60 };
                                imGuiColor = ImGui.GetColorU32(new Vector4(1f, 0.23f, 0.23f, 1f));
                            }
                            else {
                                targetColor = new ByteColor { A = 255, R = 255, G = 230, B = 90 };
                                imGuiColor = ImGui.GetColorU32(new Vector4(0.9f, 0.9f, 0.35f, 1f));
                            }

                            // Only change native colors, never modify the text string buffer!
                            nameNode->TextColor = targetColor;

                            var marketItemsNode = (AtkTextNode*)textNodes[4];
                            marketItemsNode->TextColor = targetColor;

                            int totalActions = counts.Undercuts + counts.Suboptimals;
                            string textToDraw = $"({totalActions})";

                            var pos = this.GetNodeScreenPosition(addon, (AtkResNode*)marketItemsNode);
                            var textSize = ImGui.CalcTextSize(textToDraw);

                            var drawPos = new Vector2(
                                pos.X + (((AtkResNode*)marketItemsNode)->Width * addon->Scale) - textSize.X - (10f * addon->Scale),
                                pos.Y + (((AtkResNode*)marketItemsNode)->Height * addon->Scale / 2f) - (textSize.Y / 2f)
                            );

                            this.overlayData.Add(new OverlayTextData {
                                Position = drawPos,
                                Color = imGuiColor,
                                Text = textToDraw
                            });
                        }
                    }

                    this.TraverseAndHighlight(addon, &comp->UldManager, actions);
                }
            }
        }
    }

    private void OnDrawOverlay() {
        if (this.overlayData.Count == 0) {
            return;
        }

        var addonPtr = this.gameGui.GetAddonByName("RetainerList");
        if (addonPtr.Address == IntPtr.Zero) {
            return;
        }

        unsafe {
            var addon = (AtkUnitBase*)addonPtr.Address;
            if (!addon->IsVisible) {
                return;
            }
        }

        var drawList = ImGui.GetForegroundDrawList();
        uint shadowColor = ImGui.GetColorU32(new Vector4(0f, 0f, 0f, 1f));

        foreach (var data in this.overlayData) {
            // Draw a subtle text shadow for native consistency
            drawList.AddText(new Vector2(data.Position.X + 1, data.Position.Y + 1), shadowColor, data.Text);
            drawList.AddText(data.Position, data.Color, data.Text);
        }
    }

    private unsafe void CollectVisibleTextNodes(AtkUldManager* uldManager, List<nint> list) {
        if (uldManager == null) {
            return;
        }

        for (int i = 0; i < uldManager->NodeListCount; i++) {
            var node = uldManager->NodeList[i];
            if (node == null || !node->IsVisible()) {
                continue;
            }

            if (node->Type == NodeType.Text) {
                list.Add((nint)node);
            }
            else if ((ushort)node->Type >= 1000) {
                var compNode = (AtkComponentNode*)node;
                if (compNode->Component != null) {
                    this.CollectVisibleTextNodes(&compNode->Component->UldManager, list);
                }
            }
        }
    }

    private unsafe Vector2 GetNodeScreenPosition(AtkUnitBase* addon, AtkResNode* node) {
        if (addon == null || node == null) {
            return Vector2.Zero;
        }

        float x = node->X;
        float y = node->Y;
        var parent = node->ParentNode;

        while (parent != null) {
            x += parent->X;
            y += parent->Y;
            parent = parent->ParentNode;
        }

        float screenX = addon->X + (x * addon->Scale);
        float screenY = addon->Y + (y * addon->Scale);

        return new Vector2(screenX, screenY);
    }

    private unsafe float GetAbsoluteX(nint nodePtr) {
        var node = (AtkResNode*)nodePtr;
        if (node == null) {
            return 0;
        }

        float x = node->X;
        var parent = node->ParentNode;

        while (parent != null) {
            x += parent->X;
            parent = parent->ParentNode;
        }
        return x;
    }

    private unsafe string ExtractString(byte* stringPtr) {
        if (stringPtr == null) {
            return string.Empty;
        }

        return MemoryHelper.ReadSeStringNullTerminated((nint)stringPtr).TextValue ?? string.Empty;
    }

    public void Dispose() {
        this.addonLifecycle.UnregisterListener(AddonEvent.PostUpdate, "RetainerList", this.OnRetainerListUpdate);
        this.pluginInterface.UiBuilder.Draw -= this.OnDrawOverlay;
    }
}