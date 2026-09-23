using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;
using Marketeer.Core.SalesHistory.Contracts;
using System;
using System.Collections.Generic;

namespace Marketeer.Core.SalesHistory.Services;

public class ItemResolverService : IItemResolverService {
    private IDataManager dataManager;
    private Dictionary<string, uint> nameToIdCache;
    private bool isCacheBuilt;

    public ItemResolverService(IDataManager dataManager) {
        this.dataManager = dataManager;
        this.nameToIdCache = new Dictionary<string, uint>(StringComparer.InvariantCultureIgnoreCase);
        this.isCacheBuilt = false;
    }

    private void BuildCache() {
        if (this.isCacheBuilt) return;

        var sheet = this.dataManager.GetExcelSheet<Item>();
        if (sheet == null) return;

        foreach (var item in sheet) {
            var name = item.Name.ToString();
            if (!string.IsNullOrEmpty(name)) this.nameToIdCache.TryAdd(name, item.RowId);
        }

        this.isCacheBuilt = true;
    }

    public uint ResolveItemId(string itemName) {
        if (string.IsNullOrWhiteSpace(itemName)) return 0;

        this.BuildCache();

        if (this.nameToIdCache.TryGetValue(itemName, out var cachedId)) return cachedId;

        bool isTruncated = itemName.EndsWith("...") || itemName.EndsWith("…");
        if (isTruncated) {
            string searchName = itemName.EndsWith("...")
                ? itemName.Substring(0, itemName.Length - 3).Trim()
                : itemName.Substring(0, itemName.Length - 1).Trim();

            foreach (var kvp in this.nameToIdCache) {
                if (kvp.Key.StartsWith(searchName, StringComparison.InvariantCultureIgnoreCase)) {
                    this.nameToIdCache[itemName] = kvp.Value;
                    return kvp.Value;
                }
            }
        }

        this.nameToIdCache[itemName] = 0;
        return 0;
    }

    public string ResolveItemName(uint itemId) {
        var sheet = this.dataManager.GetExcelSheet<Item>();
        var item = sheet?.GetRowOrDefault(itemId);
        return item?.Name.ToString() ?? "Unknown Item";
    }

    public uint ResolveIconId(uint itemId) {
        var sheet = this.dataManager.GetExcelSheet<Item>();
        var item = sheet?.GetRowOrDefault(itemId);
        return item?.Icon ?? 0;
    }

    public uint ResolveVendorPrice(uint itemId) {
        var sheet = this.dataManager.GetExcelSheet<Item>();
        var item = sheet?.GetRowOrDefault(itemId);
        return item?.PriceLow ?? 0;
    }

    public uint ResolveVendorBuyPrice(uint itemId) {
        var sheet = this.dataManager.GetExcelSheet<Item>();
        var item = sheet?.GetRowOrDefault(itemId);
        return item?.PriceMid ?? 0;
    }
}