using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Memory;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Graphics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Marketeer.API.CompetitionTracking.Contracts;
using Marketeer.API.GameInterop.Contracts;
using Marketeer.API.Logging.Contracts;
using Marketeer.API.MarketListings.Contracts;
using Marketeer.API.SalesHistory.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Marketeer.Core.MarketListings.Services;

public class NativeListingHighlighterService : IDisposable {
    private IAddonLifecycle addonLifecycle;
    private IGameGui gameGui;
    private IMarketListingProvider listingProvider;
    private IRetainerProvider retainerProvider;
    private ICompetitionStateService competitionState;
    private IListingOptimizationService optimizationService;
    private IInventoryService inventoryService;
    private IItemResolverService itemResolver;
    private IObjectTable objectTable;
    private ILoggerService logger;

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
            if (addon == null || !addon->IsVisible) {
                return;
            }

            var localPlayer = this.objectTable.LocalPlayer;
            if (localPlayer == null) {
                return;
            }

            var currentCharacterName = localPlayer.Name.TextValue;
            var activeId = this.listingProvider.GetActiveRetainerId();
            if (!activeId.HasValue) {
                return;
            }

            var activeRetainer = this.retainerProvider.GetActiveRetainers().FirstOrDefault(r => r.RetainerId == activeId.Value);
            if (activeRetainer == null) {
                return;
            }

            var activeRetainerName = activeRetainer.Name;
            var slots = this.inventoryService.GetInventorySlots(InventoryType.RetainerMarket);
            var allItemNames = new HashSet<string>();

            foreach (var slot in slots.Where(s => s.IsOccupied)) {
                allItemNames.Add(this.itemResolver.ResolveItemName(slot.ItemId));
            }

            if (allItemNames.Count == 0) {
                return;
            }

            var undercuts = this.competitionState.GetUndercutItems()
                .Where(u => u.CharacterName == currentCharacterName && u.RetainerName == activeRetainerName)
                .Select(u => u.ItemName)
                .ToHashSet();

            var suboptimals = this.optimizationService.GetVendorPricedListings()
                .Where(s => s.CharacterName == currentCharacterName && s.RetainerName == activeRetainerName)
                .Select(s => s.ItemName)
                .ToHashSet();

            this.TraverseAndColor(&addon->UldManager, allItemNames, undercuts, suboptimals);
        }
        catch (Exception ex) {
            this.logger.Error(ex, "Failed to apply native colors to RetainerSellList.");
        }
    }

    private unsafe void TraverseAndColor(AtkUldManager* uldManager, HashSet<string> allItems, HashSet<string> undercuts, HashSet<string> suboptimals) {
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

                    bool isRow = false;
                    bool isUndercut = false;
                    bool isSuboptimal = false;

                    foreach (var textNodePtr in textNodes) {
                        var textNode = (AtkTextNode*)textNodePtr;
                        var text = this.ExtractString(textNode->NodeText.StringPtr);

                        if (!string.IsNullOrWhiteSpace(text)) {
                            string? matchedItem = this.GetMatchingItemName(text, allItems);
                            if (matchedItem != null) {
                                isRow = true;
                                if (suboptimals.Contains(matchedItem)) {
                                    isSuboptimal = true;
                                }
                                else if (undercuts.Contains(matchedItem)) {
                                    isUndercut = true;
                                }

                                break;
                            }
                        }
                    }

                    if (isRow) {
                        ByteColor targetColor;
                        if (isSuboptimal) {
                            targetColor = new ByteColor { A = 255, R = 255, G = 60, B = 60 };
                        }
                        else if (isUndercut) {
                            targetColor = new ByteColor { A = 255, R = 255, G = 230, B = 90 };
                        }
                        else {
                            targetColor = new ByteColor { A = 255, R = 255, G = 255, B = 255 };
                        }

                        // Apply color uniformly to ALL text elements within the row (Name, Price, Qty, Total)
                        // This forces a unified visual block, overriding native item rarity colors.
                        foreach (var textNodePtr in textNodes) {
                            var textNode = (AtkTextNode*)textNodePtr;
                            textNode->TextColor = targetColor;
                        }
                    }
                    else {
                        this.TraverseAndColor(&comp->UldManager, allItems, undercuts, suboptimals);
                    }
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

    protected virtual string? GetMatchingItemName(string uiText, HashSet<string> allItems) {
        var cleanText = uiText.Replace("\uE03C", "").Trim();
        if (string.IsNullOrEmpty(cleanText)) {
            return null;
        }

        bool isTruncated = cleanText.EndsWith("...") || cleanText.EndsWith("…");
        string searchName = isTruncated ? cleanText.TrimEnd('.', '…').Trim() : cleanText;

        foreach (var item in allItems) {
            if (isTruncated) {
                if (item.StartsWith(searchName, StringComparison.InvariantCultureIgnoreCase)) {
                    return item;
                }
            }
            else if (item.Equals(searchName, StringComparison.InvariantCultureIgnoreCase)) {
                return item;
            }
        }

        return null;
    }

    private unsafe string ExtractString(byte* stringPtr) {
        if (stringPtr == null) {
            return string.Empty;
        }

        return MemoryHelper.ReadSeStringNullTerminated((nint)stringPtr).TextValue ?? string.Empty;
    }

    public void Dispose() {
        this.addonLifecycle.UnregisterListener(AddonEvent.PostUpdate, "RetainerSellList", this.OnRetainerSellListUpdate);
    }
}