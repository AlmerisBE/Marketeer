using Dalamud.Memory;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Marketeer.Features.Logging.Contracts;
using Marketeer.Features.SalesHistoryTracking.Contracts;
using Marketeer.Features.SalesHistoryTracking.Models;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Marketeer.Features.SalesHistoryTracking.Services;

public class SalesHistoryScraper : ISalesHistoryScraper {
    private IGameGui gameGui;
    private IItemResolverService itemResolver;
    private ILoggerService logger;

    public SalesHistoryScraper(
        IGameGui gameGui,
        IItemResolverService itemResolver,
        ILoggerService logger) {

        this.gameGui = gameGui;
        this.itemResolver = itemResolver;
        this.logger = logger;
    }

    public unsafe bool IsHistoryWindowOpen() {
        var addonPtr = this.gameGui.GetAddonByName("RetainerHistory");

        if (addonPtr.Address == IntPtr.Zero) {
            return false;
        }

        var addon = (AtkUnitBase*)addonPtr.Address;
        return addon->IsVisible;
    }

    public unsafe IReadOnlyList<SaleRecord> ScrapeSales() {
        var records = new List<SaleRecord>();
        var addonPtr = this.gameGui.GetAddonByName("RetainerHistory");

        if (addonPtr.Address == IntPtr.Zero) {
            this.logger.Warning("Cannot scrape sales: 'RetainerHistory' pointer is null.");
            return records;
        }

        var addon = (AtkUnitBase*)addonPtr.Address;

        if (!addon->IsVisible) {
            this.logger.Debug("ScrapeSales: Addon is not visible yet.");
            return records;
        }

        AtkComponentNode* listComponentNode = null;

        for (int i = 0; i < addon->UldManager.NodeListCount; i++) {
            var node = addon->UldManager.NodeList[i];
            if (node != null && node->NodeId == 10 && (ushort)node->Type >= 1000) {
                listComponentNode = (AtkComponentNode*)node;
                break;
            }
        }

        if (listComponentNode == null || listComponentNode->Component == null) {
            this.logger.Debug("ScrapeSales: Could not find ListComponentNode (NodeId: 10) or its Component is null.");
            return records;
        }

        var listComponent = listComponentNode->Component;
        this.logger.Debug($"ScrapeSales: Found List Component. It contains {listComponent->UldManager.NodeListCount} child nodes.");

        for (int i = 0; i < listComponent->UldManager.NodeListCount; i++) {
            var listItemNode = listComponent->UldManager.NodeList[i];

            if (listItemNode == null) {
                continue;
            }

            if ((ushort)listItemNode->Type < 1000) {
                this.logger.Debug($"Row {i} skipped: Not a component (Type: {(ushort)listItemNode->Type}).");
                continue;
            }

            if (!listItemNode->IsVisible()) {
                this.logger.Debug($"Row {i} skipped: Node is hidden.");
                continue;
            }

            var listItemComponent = ((AtkComponentNode*)listItemNode)->Component;
            if (listItemComponent == null) {
                this.logger.Debug($"Row {i} skipped: listItemComponent is null.");
                continue;
            }

            try {
                var textNodes = new Dictionary<uint, string>();
                uint quantity = 1;

                this.logger.Debug($"Row {i} processing. Child nodes count: {listItemComponent->UldManager.NodeListCount}");

                for (int j = 0; j < listItemComponent->UldManager.NodeListCount; j++) {
                    var childNode = listItemComponent->UldManager.NodeList[j];
                    if (childNode == null) {
                        continue;
                    }

                    if (childNode->Type == NodeType.Text) {
                        var text = this.ExtractString(((AtkTextNode*)childNode)->NodeText.StringPtr);

                        this.logger.Debug($"Row {i}, Child {j} (NodeId: {childNode->NodeId}): Found Text='{text}', Visible={childNode->IsVisible()}");

                        if (childNode->IsVisible() && !string.IsNullOrWhiteSpace(text)) {
                            textNodes[childNode->NodeId] = text;
                        }
                    }
                    else if ((ushort)childNode->Type >= 1000) {
                        var innerComponent = ((AtkComponentNode*)childNode)->Component;

                        if (innerComponent != null) {
                            for (int k = 0; k < innerComponent->UldManager.NodeListCount; k++) {
                                var innerChild = innerComponent->UldManager.NodeList[k];

                                if (innerChild != null && innerChild->Type == NodeType.Text && innerChild->IsVisible()) {
                                    var qtyTextNode = (AtkTextNode*)innerChild;
                                    var qtyStr = this.ExtractString(qtyTextNode->NodeText.StringPtr);

                                    var cleanQtyStr = Regex.Replace(qtyStr, @"[^\d]", "");
                                    if (uint.TryParse(cleanQtyStr, out var parsedQty) && parsedQty > 0) {
                                        quantity = parsedQty;
                                        this.logger.Debug($"Row {i}: Found nested quantity '{quantity}' inside component.");
                                    }
                                }
                            }
                        }
                    }
                }

                this.logger.Debug($"Row {i} valid text nodes mapped: {textNodes.Count}");

                if (textNodes.Count < 4) {
                    this.logger.Debug($"Row {i} skipped: Expected >= 4 texts, found {textNodes.Count}. Content: {string.Join(" | ", textNodes.Values)}");
                    continue;
                }

                string itemNameRaw = textNodes.GetValueOrDefault(3u, string.Empty);
                string priceStr = textNodes.GetValueOrDefault(6u, string.Empty);
                string buyerName = textNodes.GetValueOrDefault(7u, string.Empty);
                string dateStr = textNodes.GetValueOrDefault(8u, string.Empty);

                this.logger.Debug($"Row {i} mapping -> Item: '{itemNameRaw}', Price: '{priceStr}', Buyer: '{buyerName}', Date: '{dateStr}'");

                var (itemName, parsedQtyFromName) = this.ParseItemNameAndQuantity(itemNameRaw);
                var itemId = this.itemResolver.ResolveItemId(itemName);

                if (itemId == 0) {
                    this.logger.Debug($"Row {i}: ItemId resolution failed for exact NodeId 3. Attempting fallback mapping...");
                    foreach (var kvp in textNodes) {
                        var (fallbackName, fallbackQty) = this.ParseItemNameAndQuantity(kvp.Value);
                        itemId = this.itemResolver.ResolveItemId(fallbackName);

                        if (itemId != 0) {
                            itemNameRaw = kvp.Value;
                            if (fallbackQty > 1) {
                                parsedQtyFromName = fallbackQty;
                            }
                            this.logger.Debug($"Row {i}: Fallback resolved '{fallbackName}' to ItemId {itemId} via NodeId {kvp.Key}.");
                            break;
                        }
                    }
                }

                if (itemId == 0) {
                    this.logger.Warning($"Row {i} skipped: Could not resolve ItemID. Texts: {string.Join(" | ", textNodes.Values)}");
                    continue;
                }

                if (parsedQtyFromName > 1) {
                    quantity = parsedQtyFromName;
                }

                var cleanPrice = Regex.Replace(priceStr, @"[^\d]", "");
                if (!uint.TryParse(cleanPrice, out var unitPrice)) {
                    this.logger.Warning($"Row {i} skipped: Could not parse price '{priceStr}'");
                    continue;
                }

                if (!DateTime.TryParse(dateStr, out var saleDate)) {
                    saleDate = DateTime.UtcNow;
                }

                records.Add(new SaleRecord {
                    ItemId = itemId,
                    Quantity = quantity,
                    UnitPrice = unitPrice,
                    BuyerName = buyerName,
                    SaleDate = saleDate
                });

                this.logger.Debug($"Row {i} successfully mapped: {quantity}x ItemId {itemId} sold for {unitPrice}g.");
            }
            catch (Exception ex) {
                this.logger.Error(ex, $"Failed to parse sales history row at logical index {i}.");
            }
        }

        this.logger.Info($"Successfully scraped {records.Count} sale records from RetainerHistory natively.");
        return records;
    }

    private (string Name, uint Quantity) ParseItemNameAndQuantity(string rawText) {
        var name = rawText.Replace("", "").Trim();
        uint quantity = 1;

        var match = Regex.Match(name, @"^(.*?)\s*x(\d+)$");
        if (match.Success) {
            name = match.Groups[1].Value.Trim();
            uint.TryParse(match.Groups[2].Value, out quantity);
        }

        return (name, quantity);
    }

    private unsafe string ExtractString(byte* stringPtr) {
        if (stringPtr == null) {
            return string.Empty;
        }

        // Use Dalamud's MemoryHelper to automatically strip UI control payloads from the SeString
        return MemoryHelper.ReadSeStringNullTerminated((nint)stringPtr).TextValue ?? string.Empty;
    }
}