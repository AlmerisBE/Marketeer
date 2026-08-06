using Marketeer.API.CraftingProfit.Models;
using System.Collections.Generic;

namespace Marketeer.API.CraftingProfit.Contracts;

public interface ICraftingProfitRepository {
    IReadOnlyList<CraftingItemConfig> GetAllConfigs();
    CraftingItemConfig? GetConfig(uint itemId);
    void SaveConfig(CraftingItemConfig config);
    void RemoveConfig(uint itemId);
}