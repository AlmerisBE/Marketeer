using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;
using Marketeer.API.MarketWatch.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Marketeer.Core.GameData.Services;

public class MarketItemSearchProvider : IMarketItemSearchProvider {
    private IDataManager dataManager;
    private List<ItemSearchResult> cache;

    public MarketItemSearchProvider(IDataManager dataManager) {
        this.dataManager = dataManager;
        this.cache = new List<ItemSearchResult>();

        this.InitializeCache();
    }

    private void InitializeCache() {
        var sheet = this.dataManager.GetExcelSheet<Item>();
        if (sheet == null) {
            return;
        }

        foreach (var item in sheet) {
            // ItemSearchCategory > 0 means the item is listable on the market board
            if (item.ItemSearchCategory.RowId > 0) {
                var name = item.Name.ToString();
                if (!string.IsNullOrEmpty(name)) {
                    this.cache.Add(new ItemSearchResult {
                        ItemId = item.RowId,
                        Name = name
                    });
                }
            }
        }
    }

    public IEnumerable<ItemSearchResult> SearchMarketableItems(string query, int limit = 20) {
        if (string.IsNullOrWhiteSpace(query)) {
            return [];
        }

        return this.cache
            .Where(i => i.Name.Contains(query, StringComparison.InvariantCultureIgnoreCase))
            .Take(limit);
    }
}