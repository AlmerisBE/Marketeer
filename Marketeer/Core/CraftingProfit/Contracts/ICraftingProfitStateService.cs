using Marketeer.Core.CraftingProfit.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Marketeer.Core.CraftingProfit.Contracts;

public interface ICraftingProfitStateService {
    IReadOnlyDictionary<uint, CraftingProfitResult> Evaluations { get; }
    Task EvaluateItemAsync(uint itemId);
    Task EvaluateAllAsync();
}