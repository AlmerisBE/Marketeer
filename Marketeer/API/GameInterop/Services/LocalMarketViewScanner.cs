using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Memory;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Marketeer.API.GameInterop.Contracts;
using Marketeer.API.Universalis.Models;
using Marketeer.Core.Logging.Contracts;
using Marketeer.Core.MarketPricing.Contracts;
using Marketeer.Core.SalesHistory.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Marketeer.API.GameInterop.Services;

public class LocalMarketViewScanner : ILocalMarketViewScanner, IDisposable {
    private readonly IAddonLifecycle addonLifecycle;
    private readonly IMarketPriceCacheService priceCache;
    private readonly IItemResolverService itemResolver;
    private readonly IObjectTable objectTable;
    private readonly ILoggerService logger;

    private bool isEnabled;
    private DateTime lastScanTime;
    private uint lastScannedItemId;

    public LocalMarketViewScanner(
        IAddonLifecycle addonLifecycle,
        IMarketPriceCacheService priceCache,
        IItemResolverService itemResolver,
        IObjectTable objectTable,
        ILoggerService logger) {

        this.addonLifecycle = addonLifecycle;
        this.priceCache = priceCache;
        this.itemResolver = itemResolver;
        this.objectTable = objectTable;
        this.logger = logger;

        this.isEnabled = false;
        this.lastScanTime = DateTime.MinValue;
        this.lastScannedItemId = 0;
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
        // Enforce a strict 1.5-second debounce to prevent micro-stuttering and cache thrashing during scroll events
        if ((DateTime.Now - this.lastScanTime).TotalMilliseconds < 1500) return;

        var addon = (AtkUnitBase*)args.Addon.Address;
        if (addon == null || !addon->IsVisible) return;

        var localPlayer = this.objectTable.LocalPlayer;
        if (localPlayer == null || localPlayer.CurrentWorld.RowId == 0) return;

        uint worldId = localPlayer.CurrentWorld.RowId;

        try {
            string itemName = string.Empty;
            uint targetItemId = 0;

            for (int i = 0; i < addon->UldManager.NodeListCount; i++) {
                var node = addon->UldManager.NodeList[i];
                if (node != null && node->Type == NodeType.Text && node->IsVisible()) {
                    var textNode = (AtkTextNode*)node;
                    var text = this.ExtractString((nint)(byte*)textNode->NodeText.StringPtr).Trim();

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

            // Debounce matching same-item spam unless 5 seconds have passed
            if (targetItemId == this.lastScannedItemId && (DateTime.Now - this.lastScanTime).TotalSeconds < 5) return;

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
                this.priceCache.UpdateLocalPrices(targetItemId, worldId, results);
                this.lastScanTime = DateTime.Now;
                this.lastScannedItemId = targetItemId;
            }
        }
        catch (Exception ex) {
            this.logger.Error(ex, "Failed to scrape live market board data from ItemSearchResult.");
        }
    }

    private unsafe void ExtractListingsFromComponent(AtkUldManager* uldManager, uint baseItemId, List<LowestPriceResult> results) {
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
                            var ptr = (byte*)textNode->NodeText.StringPtr;
                            var text = this.ExtractString((nint)ptr);

                            if (!string.IsNullOrWhiteSpace(text)) textNodes.Add((this.GetAbsoluteX((nint)childNode), text));
                        }
                        else if (childNode->Type == NodeType.Image) {
                            if (this.GetAbsoluteX((nint)childNode) < 50f) isHq = true;
                        }
                    }

                    textNodes = textNodes.OrderBy(t => t.X).ToList();

                    if (textNodes.Count >= 4) {
                        var priceStr = new string(textNodes[0].Text.Where(char.IsDigit).ToArray());
                        var retainerName = textNodes.Last().Text.Trim();

                        if (uint.TryParse(priceStr, out var price) && !string.IsNullOrWhiteSpace(retainerName)) {
                            uint actualItemId = isHq ? baseItemId + 1000000u : baseItemId;
                            results.Add(new LowestPriceResult {
                                ItemId = actualItemId,
                                Price = price,
                                RetainerName = retainerName,
                                IsHq = isHq
                            });
                        }
                    }

                    this.ExtractListingsFromComponent(&rowComponent->UldManager, baseItemId, results);
                }
            }
        }
    }

    private unsafe string ExtractString(nint stringPtr) {
        if (stringPtr == IntPtr.Zero) return string.Empty;

        try {
            return MemoryHelper.ReadSeStringNullTerminated(stringPtr).TextValue ?? string.Empty;
        }
        catch (Exception) {
            return MemoryHelper.ReadStringNullTerminated(stringPtr);
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

    public void Dispose() {
        this.Disable();
    }
}