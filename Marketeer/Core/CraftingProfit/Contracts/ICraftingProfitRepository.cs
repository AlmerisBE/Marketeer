using Marketeer.Core.CraftingProfit.Models;
using System.Collections.Generic;

namespace Marketeer.Core.CraftingProfit.Contracts;

public interface ICraftingProfitRepository {
    IReadOnlyList<CraftingItemConfig> GetAllConfigs();
    CraftingItemConfig? GetConfig(uint itemId);
    void SaveConfig(CraftingItemConfig config);
    void RemoveConfig(uint itemId);
}