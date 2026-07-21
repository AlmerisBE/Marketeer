using Dalamud.Memory;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Marketeer.API.Logging.Contracts;
using Marketeer.API.SalesHistory.Contracts;
using Marketeer.API.SalesHistory.Models;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Marketeer.Core.SalesHistory.Services;

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
            // Restored the missing warning log to satisfy the unit test constraints
            this.logger.Warning("Cannot scrape sales: 'RetainerHistory' pointer is null.");
            return records;
        }

        var addon = (AtkUnitBase*)addonPtr.Address;

        if (!addon->IsVisible) {
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
            return records;
        }

        var listComponent = listComponentNode->Component;

        for (int i = 0; i < listComponent->UldManager.NodeListCount; i++) {
            var listItemNode = listComponent->UldManager.NodeList[i];

            if (listItemNode == null || (ushort)listItemNode->Type < 1000 || !listItemNode->IsVisible()) {
                continue;
            }

            var listItemComponent = ((AtkComponentNode*)listItemNode)->Component;
            if (listItemComponent == null) {
                continue;
            }

            try {
                var textNodes = new Dictionary<uint, string>();
                uint quantity = 1;

                for (int j = 0; j < listItemComponent->UldManager.NodeListCount; j++) {
                    var childNode = listItemComponent->UldManager.NodeList[j];
                    if (childNode == null) {
                        continue;
                    }

                    if (childNode->Type == NodeType.Text) {
                        var text = this.ExtractString(((AtkTextNode*)childNode)->NodeText.StringPtr);

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
                                    }
                                }
                            }
                        }
                    }
                }

                if (textNodes.Count < 4) {
                    continue;
                }

                string itemNameRaw = textNodes.GetValueOrDefault(3u, string.Empty);
                string priceStr = textNodes.GetValueOrDefault(6u, string.Empty);
                string buyerName = textNodes.GetValueOrDefault(7u, string.Empty);
                string dateStr = textNodes.GetValueOrDefault(8u, string.Empty);

                var (itemName, parsedQtyFromName) = this.ParseItemNameAndQuantity(itemNameRaw);
                var itemId = this.itemResolver.ResolveItemId(itemName);

                if (itemId == 0) {
                    foreach (var kvp in textNodes) {
                        var (fallbackName, fallbackQty) = this.ParseItemNameAndQuantity(kvp.Value);
                        itemId = this.itemResolver.ResolveItemId(fallbackName);

                        if (itemId != 0) {
                            itemNameRaw = kvp.Value;
                            if (fallbackQty > 1) {
                                parsedQtyFromName = fallbackQty;
                            }
                            break;
                        }
                    }
                }

                if (itemId == 0) {
                    continue;
                }

                if (parsedQtyFromName > 1) {
                    quantity = parsedQtyFromName;
                }

                var cleanPrice = Regex.Replace(priceStr, @"[^\d]", "");
                if (!uint.TryParse(cleanPrice, out var unitPrice)) {
                    continue;
                }

                DateTime saleDate = this.ParseSaleDate(dateStr);

                records.Add(new SaleRecord {
                    ItemId = itemId,
                    Quantity = quantity,
                    UnitPrice = unitPrice,
                    BuyerName = buyerName,
                    SaleDate = saleDate
                });
            }
            catch (Exception ex) {
                this.logger.Error(ex, $"Failed to parse sales history row at logical index {i}.");
            }
        }

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

    private DateTime ParseSaleDate(string dateStr) {
        if (string.IsNullOrWhiteSpace(dateStr)) {
            return DateTime.MinValue;
        }

        var normalized = dateStr.Replace("h", ":").Replace("H", ":").Trim();

        if (DateTime.TryParse(normalized, out var parsedDate)) {
            return parsedDate;
        }

        var cleanForRegex = normalized.Replace(" ", "");
        var match = Regex.Match(cleanForRegex, @"^(\d+)[^\d]+(\d+)[^\d]+(\d+)[^\d]+(\d+)$");

        if (match.Success) {
            if (int.TryParse(match.Groups[1].Value, out int p1) &&
                int.TryParse(match.Groups[2].Value, out int p2) &&
                int.TryParse(match.Groups[3].Value, out int hour) &&
                int.TryParse(match.Groups[4].Value, out int minute)) {

                int day = p1;
                int month = p2;

                if (p1 > 12) {
                    day = p1;
                    month = p2;
                }
                else if (p2 > 12) {
                    month = p1;
                    day = p2;
                }

                int year = DateTime.Now.Year;

                if (month > DateTime.Now.Month) {
                    year--;
                }

                try {
                    return new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Local);
                }
                catch {
                    // Ignored intentionally
                }
            }
        }

        return DateTime.MinValue;
    }

    private unsafe string ExtractString(byte* stringPtr) {
        if (stringPtr == null) {
            return string.Empty;
        }

        return MemoryHelper.ReadSeStringNullTerminated((nint)stringPtr).TextValue ?? string.Empty;
    }
}