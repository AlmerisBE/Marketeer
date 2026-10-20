using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Memory;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Graphics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Marketeer.API.GameInterop.Contracts;
using Marketeer.Core.Logging.Contracts;
using Marketeer.Core.MarketListings.Contracts;
using Marketeer.Core.MarketPricing.Models;
using Marketeer.Core.SalesHistory.Contracts;
using Marketeer.UI.CompetitionTracking.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Marketeer.UI.RetainerOverlays.Services;

public class NativeListingHighlighterService : IDisposable {
    private readonly IAddonLifecycle addonLifecycle;
    private readonly IGameGui gameGui;
    private readonly IMarketListingProvider listingProvider;
    private readonly IRetainerProvider retainerProvider;
    private readonly ICompetitionStateService competitionState;
    private readonly IListingOptimizationService optimizationService;
    private readonly IInventoryService inventoryService;
    private readonly IItemResolverService itemResolver;
    private readonly IObjectTable objectTable;
    private readonly ILoggerService logger;

    public NativeListingHighlighterService(
        IAddonLifecycle addonLifecycle,
        IGameGui gameGui,
        IMarketListingProvider listingProvider,
        IRetainerProvider retainerProvider,
        ICompetitionStateService competitionState,
        IListingOptimizationService optimizationService,
        IInventoryService inventoryService,
        IItemResolverService itemResolver,
        IObjectTable objectTable,
        ILoggerService logger) {

        this.addonLifecycle = addonLifecycle;
        this.gameGui = gameGui;
        this.listingProvider = listingProvider;
        this.retainerProvider = retainerProvider;
        this.competitionState = competitionState;
        this.optimizationService = optimizationService;
        this.inventoryService = inventoryService;
        this.itemResolver = itemResolver;
        this.objectTable = objectTable;
        this.logger = logger;

        this.addonLifecycle.RegisterListener(AddonEvent.PostUpdate, "RetainerSellList", this.OnRetainerSellListUpdate);
    }

    private unsafe void OnRetainerSellListUpdate(AddonEvent type, AddonArgs args) {
        try {
            var addon = (AtkUnitBase*)args.Addon.Address;
            if (addon == null || !addon->IsVisible) return;

            var localPlayer = this.objectTable.LocalPlayer;
            if (localPlayer == null) return;

            var currentCharacterName = localPlayer.Name.TextValue;
            var currentWorldId = localPlayer.HomeWorld.RowId;
            var activeId = this.listingProvider.GetActiveRetainerId();
            if (!activeId.HasValue) return;

            var activeRetainer = this.retainerProvider.GetActiveRetainers().FirstOrDefault(r => r.RetainerId == activeId.Value);
            if (activeRetainer == null) return;

            var activeRetainerName = activeRetainer.Name;
            var slots = this.inventoryService.GetInventorySlots(InventoryType.RetainerMarket);
            var allItemNames = new HashSet<string>();

            foreach (var slot in slots.Where(s => s.IsOccupied)) {
                allItemNames.Add(this.itemResolver.ResolveItemName(slot.ItemId));
            }

            var actionItems = new HashSet<(string ItemName, uint Price, uint Quantity, PricingAction Action)>();

            if (allItemNames.Count > 0) {
                var undercuts = this.competitionState.GetUndercutItems()
                    .Where(u => u.CharacterName == currentCharacterName && u.HomeWorldId == currentWorldId && u.RetainerName == activeRetainerName);

                foreach (var u in undercuts) {
                    actionItems.Add((u.ItemName, u.Price, u.Quantity, u.SuggestedAction));
                }

                var suboptimals = this.optimizationService.GetVendorPricedListings()
                    .Where(s => s.CharacterName == currentCharacterName && s.HomeWorldId == currentWorldId && s.RetainerName == activeRetainerName);

                foreach (var s in suboptimals) {
                    actionItems.Add((s.ItemName, s.Price, s.Quantity, PricingAction.CancelListing));
                }
            }

            this.TraverseAndHighlight(&addon->UldManager, allItemNames, actionItems);
        }
        catch (Exception ex) {
            this.logger.Error(ex, "Failed to apply native colors to RetainerSellList.");
        }
    }

    private unsafe void TraverseAndHighlight(AtkUldManager* uldManager, HashSet<string> allItems, HashSet<(string ItemName, uint Price, uint Quantity, PricingAction Action)> actionItems) {
        if (uldManager == null) return;

        for (int i = 0; i < uldManager->NodeListCount; i++) {
            var node = uldManager->NodeList[i];
            if (node == null || !node->IsVisible()) continue;

            if ((ushort)node->Type >= 1000) {
                var compNode = (AtkComponentNode*)node;
                var comp = compNode->Component;

                if (comp != null) {
                    var textNodes = new List<nint>();
                    this.CollectVisibleTextNodes(&comp->UldManager, textNodes);

                    string? matchedItem = null;
                    var rowNumbers = new List<uint>();

                    foreach (var textNodePtr in textNodes) {
                        var textNode = (AtkTextNode*)textNodePtr;
                        var stringPtr = (byte*)textNode->NodeText.StringPtr;
                        if (stringPtr == null) continue;

                        var text = this.ExtractString((nint)stringPtr);

                        if (!string.IsNullOrWhiteSpace(text)) {
                            if (matchedItem == null) matchedItem = this.GetMatchingItemName(text, allItems);

                            var digits = new string(text.Where(char.IsDigit).ToArray());
                            if (!string.IsNullOrEmpty(digits) && uint.TryParse(digits, out var parsedNum)) {
                                rowNumbers.Add(parsedNum);
                            }
                        }
                    }

                    if (matchedItem != null) {
                        PricingAction? matchedAction = null;

                        foreach (var item in actionItems) {
                            if (item.ItemName == matchedItem && rowNumbers.Contains(item.Price) && rowNumbers.Contains(item.Quantity)) {
                                matchedAction = item.Action;
                                break;
                            }
                        }

                        if (matchedAction.HasValue) {
                            ByteColor targetTextColor;
                            if (matchedAction.Value == PricingAction.CancelListing) targetTextColor = new ByteColor { A = 255, R = 255, G = 60, B = 60 };
                            else if (matchedAction.Value == PricingAction.IncreasePrice) targetTextColor = new ByteColor { A = 255, R = 100, G = 200, B = 255 };
                            else targetTextColor = new ByteColor { A = 255, R = 255, G = 230, B = 90 };

                            foreach (var textNodePtr in textNodes) {
                                var textNode = (AtkTextNode*)textNodePtr;
                                textNode->TextColor = targetTextColor;
                                textNode->AtkResNode.Color = new ByteColor { A = 255, R = 255, G = 255, B = 255 };

                                this.StripColorPayloadsSafe(textNode);
                            }
                        }
                        else this.ResetTextColor(textNodes);
                    }

                    this.TraverseAndHighlight(&comp->UldManager, allItems, actionItems);
                }
            }
        }
    }

    private unsafe void ResetTextColor(List<nint> textNodes) {
        foreach (var textNodePtr in textNodes) {
            var textNode = (AtkTextNode*)textNodePtr;
            textNode->TextColor = new ByteColor { A = 255, R = 255, G = 255, B = 255 };
            textNode->AtkResNode.Color = new ByteColor { A = 255, R = 255, G = 255, B = 255 };
        }
    }

    private unsafe void StripColorPayloadsSafe(AtkTextNode* textNode) {
        if (textNode == null) return;

        var stringPtr = (byte*)textNode->NodeText.StringPtr;
        if (stringPtr == null) return;

        var seString = MemoryHelper.ReadSeStringNullTerminated((nint)stringPtr);
        bool hasColor = seString.Payloads.Any(p => p.Type == Dalamud.Game.Text.SeStringHandling.PayloadType.UIForeground || p.Type == Dalamud.Game.Text.SeStringHandling.PayloadType.UIGlow);

        if (hasColor) {
            seString.Payloads.RemoveAll(p => p.Type == Dalamud.Game.Text.SeStringHandling.PayloadType.UIForeground || p.Type == Dalamud.Game.Text.SeStringHandling.PayloadType.UIGlow);
            var encoded = seString.Encode().Concat(new byte[] { 0 }).ToArray();

            fixed (byte* ptr = encoded) textNode->SetText(ptr);
        }
    }

    private unsafe void CollectVisibleTextNodes(AtkUldManager* uldManager, List<nint> list) {
        if (uldManager == null) return;

        for (int i = 0; i < uldManager->NodeListCount; i++) {
            var node = uldManager->NodeList[i];
            if (node == null || !node->IsVisible()) continue;

            if (node->Type == NodeType.Text) list.Add((nint)node);
            else if ((ushort)node->Type >= 1000) {
                var compNode = (AtkComponentNode*)node;
                if (compNode->Component != null) {
                    this.CollectVisibleTextNodes(&compNode->Component->UldManager, list);
                }
            }
        }
    }

    protected virtual string? GetMatchingItemName(string uiText, HashSet<string> allItems) {
        var cleanText = uiText.Replace("\uE03C", "").Replace("", "").Trim();
        if (string.IsNullOrEmpty(cleanText)) return null;

        bool isTruncated = cleanText.EndsWith("...") || cleanText.EndsWith("…");
        string searchName = isTruncated ? cleanText.TrimEnd('.', '…').Trim() : cleanText;

        foreach (var item in allItems) {
            if (isTruncated) {
                if (item.StartsWith(searchName, StringComparison.InvariantCultureIgnoreCase)) return item;
            }
            else if (item.Equals(searchName, StringComparison.InvariantCultureIgnoreCase)) return item;
        }

        return null;
    }

    private unsafe string ExtractString(nint stringPtr) {
        if (stringPtr == IntPtr.Zero) return string.Empty;

        try {
            return MemoryHelper.ReadSeStringNullTerminated(stringPtr).TextValue ?? string.Empty;
        }
        catch (Exception) {
            return string.Empty;
        }
    }

    public void Dispose() {
        this.addonLifecycle.UnregisterListener(AddonEvent.PostUpdate, "RetainerSellList", this.OnRetainerSellListUpdate);
    }
}