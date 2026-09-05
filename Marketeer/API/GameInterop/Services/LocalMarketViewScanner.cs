using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Memory;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Marketeer.API.GameInterop.Contracts;
using Marketeer.API.Universalis.Contracts;
using Marketeer.API.Universalis.Models;
using Marketeer.Core.Logging.Contracts;
using Marketeer.Core.SalesHistory.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Marketeer.API.GameInterop.Services;

public class LocalMarketViewScanner : ILocalMarketViewScanner, IDisposable {
    private readonly IAddonLifecycle addonLifecycle;
    private readonly IServerPriceProvider priceProvider;
    private readonly IItemResolverService itemResolver;
    private readonly IObjectTable objectTable;
    private readonly ILoggerService logger;

    private bool isEnabled;
    private DateTime lastScanTime;

    public LocalMarketViewScanner(
        IAddonLifecycle addonLifecycle,
        IServerPriceProvider priceProvider,
        IItemResolverService itemResolver,
        IObjectTable objectTable,
        ILoggerService logger) {

        this.addonLifecycle = addonLifecycle;
        this.priceProvider = priceProvider;
        this.itemResolver = itemResolver;
        this.objectTable = objectTable;
        this.logger = logger;

        this.isEnabled = false;
        this.lastScanTime = DateTime.MinValue;
    }

    public void Enable() {
        if (this.isEnabled) return;

        this.addonLifecycle.RegisterListener(AddonEvent.PostUpdate, "ItemSearchResult", this.OnAddonUpdate);
        this.isEnabled = true;
        this.logger.Debug("LocalMarketViewScanner enabled. Listening for live market board data.");
    }

    public void Disable() {
        if (!this.isEnabled) return;

        this.addonLifecycle.UnregisterListener(AddonEvent.PostUpdate, "ItemSearchResult", this.OnAddonUpdate);
        this.isEnabled = false;
    }

    private unsafe void OnAddonUpdate(AddonEvent type, AddonArgs args) {
        if ((DateTime.Now - this.lastScanTime).TotalMilliseconds < 1000) return; // Anti-spam debounce

        var addon = (AtkUnitBase*)args.Addon.Address;
        if (addon == null || !addon->IsVisible) return;

        var localPlayer = this.objectTable.LocalPlayer;
        if (localPlayer == null) return;

        uint worldId = localPlayer.CurrentWorld.RowId;
        if (worldId == 0) return;

        try {
            string itemName = string.Empty;
            uint targetItemId = 0;

            // 1. Locate the Item Name in the addon's root text nodes
            for (int i = 0; i < addon->UldManager.NodeListCount; i++) {
                var node = addon->UldManager.NodeList[i];
                if (node != null && node->Type == NodeType.Text && node->IsVisible()) {
                    var textNode = (AtkTextNode*)node;
                    var text = this.ExtractString((byte*)textNode->NodeText.StringPtr).Trim();

                    if (!string.IsNullOrWhiteSpace(text)) {
                        uint resolvedId = this.itemResolver.ResolveItemId(text);
                        if (resolvedId > 0) {
                            itemName = text;
                            targetItemId = resolvedId;
                            break;
                        }
                    }
                }
            }

            if (targetItemId == 0) return;

            // 2. Locate the List Component and parse the rows
            var results = new List<LowestPriceResult>();

            for (int i = 0; i < addon->UldManager.NodeListCount; i++) {
                var node = addon->UldManager.NodeList[i];
                if (node != null && (ushort)node->Type >= 1000 && node->IsVisible()) {
                    var compNode = (AtkComponentNode*)node;
                    if (compNode->Component != null) {
                        this.ExtractListingsFromComponent(&compNode->Component->UldManager, targetItemId, results);
                    }
                }
            }

            if (results.Count > 0) {
                this.priceProvider.UpdateLocalCache(targetItemId, worldId, results);
                this.lastScanTime = DateTime.Now;
            }
        }
        catch (Exception ex) {
            this.logger.Error(ex, "Failed to scrape live market board data from ItemSearchResult.");
        }
    }

    private unsafe void ExtractListingsFromComponent(AtkUldManager* uldManager, uint itemId, List<LowestPriceResult> results) {
        if (uldManager == null) return;

        for (int i = 0; i < uldManager->NodeListCount; i++) {
            var node = uldManager->NodeList[i];
            if (node == null || !node->IsVisible()) continue;

            if ((ushort)node->Type >= 1000) {
                var listRowNode = (AtkComponentNode*)node;
                var rowComponent = listRowNode->Component;

                if (rowComponent != null) {
                    var textNodes = new List<(float X, string Text)>();
                    bool isHq = false;

                    for (int j = 0; j < rowComponent->UldManager.NodeListCount; j++) {
                        var childNode = rowComponent->UldManager.NodeList[j];
                        if (childNode == null || !childNode->IsVisible()) continue;

                        if (childNode->Type == NodeType.Text) {
                            var textNode = (AtkTextNode*)childNode;
                            var text = this.ExtractString((byte*)textNode->NodeText.StringPtr);
                            if (!string.IsNullOrWhiteSpace(text)) {
                                textNodes.Add((this.GetAbsoluteX((nint)childNode), text));
                            }
                        }
                        else if (childNode->Type == NodeType.Image) {
                            // Basic heuristic: if an image node is visible early in the row, it's likely the HQ icon.
                            // The exact icon ID can vary, but its presence in the first column is a strong indicator.
                            if (this.GetAbsoluteX((nint)childNode) < 50f) {
                                isHq = true;
                            }
                        }
                    }

                    // Order by X position: 0=Price, 1=Qty, 2=Total, 3=RetainerName
                    textNodes = textNodes.OrderBy(t => t.X).ToList();

                    if (textNodes.Count >= 4) {
                        var priceStr = new string(textNodes[0].Text.Where(char.IsDigit).ToArray());
                        var retainerName = textNodes.Last().Text.Trim();

                        if (uint.TryParse(priceStr, out var price) && !string.IsNullOrWhiteSpace(retainerName)) {
                            results.Add(new LowestPriceResult {
                                ItemId = itemId,
                                Price = price,
                                RetainerName = retainerName,
                                IsHq = isHq
                            });
                        }
                    }

                    // Recursive search for nested lists if any
                    this.ExtractListingsFromComponent(&rowComponent->UldManager, itemId, results);
                }
            }
        }
    }

    private unsafe float GetAbsoluteX(nint nodePtr) {
        var node = (AtkResNode*)nodePtr;
        if (node == null) return 0;

        float x = node->X;
        var parent = node->ParentNode;
        while (parent != null) {
            x += parent->X;
            parent = parent->ParentNode;
        }
        return x;
    }

    private unsafe string ExtractString(byte* stringPtr) {
        if (stringPtr == null) return string.Empty;
        return MemoryHelper.ReadSeStringNullTerminated((nint)stringPtr).TextValue ?? string.Empty;
    }

    public void Dispose() {
        this.Disable();
    }
}