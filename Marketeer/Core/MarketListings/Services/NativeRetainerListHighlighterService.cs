using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Memory;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Graphics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Marketeer.API.CompetitionTracking.Contracts;
using Marketeer.API.Logging.Contracts;
using Marketeer.API.MarketListings.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Marketeer.Core.MarketListings.Services;

public class NativeRetainerListHighlighterService : IDisposable {
    private IAddonLifecycle addonLifecycle;
    private ICompetitionStateService competitionState;
    private IListingOptimizationService optimizationService;
    private IObjectTable objectTable;
    private ILoggerService logger;

    public NativeRetainerListHighlighterService(
        IAddonLifecycle addonLifecycle,
        ICompetitionStateService competitionState,
        IListingOptimizationService optimizationService,
        IObjectTable objectTable,
        ILoggerService logger) {

        this.addonLifecycle = addonLifecycle;
        this.competitionState = competitionState;
        this.optimizationService = optimizationService;
        this.objectTable = objectTable;
        this.logger = logger;

        this.addonLifecycle.RegisterListener(AddonEvent.PostUpdate, "RetainerList", this.OnRetainerListUpdate);
    }

    private unsafe void OnRetainerListUpdate(AddonEvent type, AddonArgs args) {
        try {
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

            this.TraverseAndHighlight(&addon->UldManager, retainerActions);
        }
        catch (Exception ex) {
            this.logger.Error(ex, "Failed to apply native highlights to RetainerList.");
        }
    }

    private unsafe void TraverseAndHighlight(AtkUldManager* uldManager, Dictionary<string, (int Undercuts, int Suboptimals)> actions) {
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
                        var rawNameText = this.ExtractString(nameNode->NodeText.StringPtr);
                        var cleanNameText = rawNameText.Trim();

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

                            if (counts.Suboptimals > 0 && counts.Undercuts > 0) {
                                targetColor = new ByteColor { A = 255, R = 255, G = 150, B = 50 };
                            }
                            else if (counts.Suboptimals > 0) {
                                targetColor = new ByteColor { A = 255, R = 255, G = 60, B = 60 };
                            }
                            else {
                                targetColor = new ByteColor { A = 255, R = 255, G = 230, B = 90 };
                            }

                            nameNode->TextColor = targetColor;

                            var marketItemsNode = (AtkTextNode*)textNodes[4];
                            var marketResNode = (AtkResNode*)marketItemsNode;

                            // Expanding the width artificially prevents the FFXIV engine from creating truncation artifacts!
                            if (marketResNode->Width < 200) {
                                marketResNode->Width = 200;
                            }

                            marketItemsNode->TextColor = targetColor;

                            var rawMarketText = this.ExtractString(marketItemsNode->NodeText.StringPtr);
                            var cleanMarketText = rawMarketText.Split('(')[0].Replace("=", "").TrimEnd(' ', '…', '.');

                            int totalActions = counts.Undercuts + counts.Suboptimals;
                            string newTextStr = $"{cleanMarketText} ({totalActions})";

                            var encoded = Encoding.UTF8.GetBytes(newTextStr).Concat(new byte[] { 0 }).ToArray();
                            bool needsUpdate = true;

                            if (marketItemsNode->NodeText.BufUsed >= encoded.Length) {
                                var currentBytes = new ReadOnlySpan<byte>(marketItemsNode->NodeText.StringPtr, encoded.Length);
                                if (currentBytes.SequenceEqual(encoded)) {
                                    needsUpdate = false;
                                }
                            }

                            if (needsUpdate) {
                                fixed (byte* ptr = encoded) {
                                    marketItemsNode->SetText(ptr);
                                }
                            }
                        }
                    }

                    this.TraverseAndHighlight(&comp->UldManager, actions);
                }
            }
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
    }
}