using Marketeer.API.CraftingProfit.Models;
using System.Threading.Tasks;

namespace Marketeer.API.CraftingProfit.Contracts;

public interface ICraftingCostEvaluator {
    Task<CraftingProfitResult> EvaluateAsync(CraftingItemConfig config, uint worldId);
}