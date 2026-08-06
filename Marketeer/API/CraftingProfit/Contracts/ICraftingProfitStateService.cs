using Marketeer.API.CraftingProfit.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Marketeer.API.CraftingProfit.Contracts;

public interface ICraftingProfitStateService {
    IReadOnlyDictionary<uint, CraftingProfitResult> Evaluations { get; }
    Task EvaluateItemAsync(uint itemId);
    Task EvaluateAllAsync();
}