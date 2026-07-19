using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;
using Marketeer.Features.SalesHistoryTracking.Contracts;
using System;
using System.Collections.Generic;

namespace Marketeer.Features.SalesHistoryTracking.Services;

public class ItemResolverService : IItemResolverService {
    private IDataManager dataManager;
    private Dictionary<string, uint> nameToIdCache;

    public ItemResolverService(IDataManager dataManager) {
        this.dataManager = dataManager;
        this.nameToIdCache = new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase);
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

        foreach (var item in sheet) {
            var name = item.Name.ToString();
            if (name.Equals(itemName, StringComparison.OrdinalIgnoreCase)) {
                this.nameToIdCache[itemName] = item.RowId;
                return item.RowId;
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
}