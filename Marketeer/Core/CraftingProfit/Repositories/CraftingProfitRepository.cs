using Marketeer.API.Configuration.Contracts;
using Marketeer.API.CraftingProfit.Contracts;
using Marketeer.API.CraftingProfit.Models;
using System.Collections.Generic;
using System.Linq;

namespace Marketeer.Core.CraftingProfit.Repositories;

public class CraftingProfitRepository : ICraftingProfitRepository {
    private IConfigurationService configService;

    public CraftingProfitRepository(IConfigurationService configService) {
        this.configService = configService;

        if (this.configService.GetConfig().CraftingItems == null) {
            this.configService.GetConfig().CraftingItems = new Dictionary<uint, CraftingItemConfig>();
        }
    }

    public IReadOnlyList<CraftingItemConfig> GetAllConfigs() {
        return this.configService.GetConfig().CraftingItems.Values.ToList().AsReadOnly();
    }

    public CraftingItemConfig? GetConfig(uint itemId) {
        if (this.configService.GetConfig().CraftingItems.TryGetValue(itemId, out var config)) {
            return config;
        }
        return null;
    }

    public void SaveConfig(CraftingItemConfig config) {
        if (config == null || config.ItemId == 0) {
            return;
        }

        this.configService.GetConfig().CraftingItems[config.ItemId] = config;
        this.configService.Save();
    }

    public void RemoveConfig(uint itemId) {
        if (this.configService.GetConfig().CraftingItems.Remove(itemId)) {
            this.configService.Save();
        }
    }
}