using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;
using Marketeer.Core.SalesHistory.Contracts;
using System;
using System.Collections.Generic;

namespace Marketeer.Core.SalesHistory.Services;

public class ItemResolverService : IItemResolverService {
    private IDataManager dataManager;
    private Dictionary<string, uint> nameToIdCache;

    public ItemResolverService(IDataManager dataManager) {
        this.dataManager = dataManager;
        this.nameToIdCache = new Dictionary<string, uint>(StringComparer.InvariantCultureIgnoreCase);
    }

    public uint ResolveItemId(string itemName) {
        if (string.IsNullOrWhiteSpace(itemName)) {
            return 0;
        }

        if (this.nameToIdCache.TryGetValue(itemName, out var cachedId)) {
            return cachedId;
        }

        var sheet = this.dataManager.GetExcelSheet<Item>();
        if (sheet == null) {
            return 0;
        }

        bool isTruncated = itemName.EndsWith("...") || itemName.EndsWith("…");
        string searchName = itemName;

        if (isTruncated) {
            if (itemName.EndsWith("...")) {
                searchName = itemName.Substring(0, itemName.Length - 3).Trim();
            }
            else {
                searchName = itemName.Substring(0, itemName.Length - 1).Trim();
            }
        }

        foreach (var item in sheet) {
            var name = item.Name.ToString();

            if (isTruncated) {
                if (name.StartsWith(searchName, StringComparison.InvariantCultureIgnoreCase)) {
                    this.nameToIdCache[itemName] = item.RowId;
                    return item.RowId;
                }
            }
            else {
                if (name.Equals(itemName, StringComparison.InvariantCultureIgnoreCase)) {
                    this.nameToIdCache[itemName] = item.RowId;
                    return item.RowId;
                }
            }
        }

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
}