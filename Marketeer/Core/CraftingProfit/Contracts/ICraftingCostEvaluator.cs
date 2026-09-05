using Marketeer.Core.CraftingProfit.Models;
using System.Threading.Tasks;

namespace Marketeer.Core.CraftingProfit.Contracts;

public interface ICraftingCostEvaluator {
    Task<CraftingProfitResult> EvaluateAsync(CraftingItemConfig config, uint worldId);
}