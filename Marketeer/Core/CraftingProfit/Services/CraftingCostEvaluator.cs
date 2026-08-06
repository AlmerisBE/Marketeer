using Marketeer.API.CraftingProfit.Contracts;
using Marketeer.API.CraftingProfit.Models;
using Marketeer.API.GameData.Contracts;
using Marketeer.API.GameData.Models;
using Marketeer.API.Logging.Contracts;
using Marketeer.API.Universalis.Contracts;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Marketeer.Core.CraftingProfit.Services;

public class CraftingCostEvaluator : ICraftingCostEvaluator {
    private IServerPriceProvider priceProvider;
    private IRecipeDataService recipeDataService;
    private ILoggerService logger;

    public CraftingCostEvaluator(IServerPriceProvider priceProvider, IRecipeDataService recipeDataService, ILoggerService logger) {
        this.priceProvider = priceProvider;
        this.recipeDataService = recipeDataService;
        this.logger = logger;
    }

    public async Task<CraftingProfitResult> EvaluateAsync(CraftingItemConfig config, uint worldId) {
        var result = new CraftingProfitResult {
            ItemId = config.ItemId,
            TargetSellPrice = config.TargetSellPrice
        };

        try {
            var currentMarketData = await this.priceProvider.GetLowestPriceAsync(config.ItemId, worldId, false);
            result.CurrentMarketPrice = currentMarketData?.Price ?? 0;

            var recipe = this.recipeDataService.GetPrimaryRecipe(config.ItemId);
            if (recipe != null) {
                result.ResultQuantity = Math.Max(1u, recipe.ResultQuantity);
                result.ComponentEvaluations = await this.EvaluateComponentsAsync(recipe.Ingredients, config.Components, worldId);

                uint batchCost = 0;
                uint batchTargetCost = 0;

                foreach (var eval in result.ComponentEvaluations) {
                    batchCost += eval.TotalCost;
                    batchTargetCost += eval.TotalTargetCost;
                }

                result.BatchCraftingCost = batchCost;
                result.TotalCraftingCost = (uint)Math.Ceiling((double)batchCost / result.ResultQuantity);

                result.BatchTargetCraftingCost = batchTargetCost;
                result.TotalTargetCraftingCost = (uint)Math.Ceiling((double)batchTargetCost / result.ResultQuantity);
            }
        }
        catch (Exception ex) {
            this.logger.Error(ex, $"Failed to evaluate crafting cost for item {config.ItemId}");
        }

        return result;
    }

    private async Task<List<ComponentEvaluation>> EvaluateComponentsAsync(
        IEnumerable<RecipeIngredient> ingredients,
        Dictionary<uint, ComponentConfig> userConfig,
        uint worldId) {

        var evaluations = new List<ComponentEvaluation>();

        foreach (var ingredient in ingredients) {
            var evaluation = new ComponentEvaluation {
                ItemId = ingredient.ItemId,
                QuantityRequired = ingredient.Quantity
            };

            userConfig.TryGetValue(ingredient.ItemId, out var compConfig);
            bool ignoreCost = compConfig != null && compConfig.IgnoreCost;
            bool craftRecursively = compConfig != null && compConfig.CraftRecursively;

            evaluation.IsCostIgnored = ignoreCost;

            if (compConfig != null && compConfig.TargetBuyPrice > 0) {
                evaluation.TargetUnitCost = compConfig.TargetBuyPrice;
            }

            if (ignoreCost) {
                evaluation.UnitCost = 0;
                evaluation.TargetUnitCost = 0;
            }
            else if (craftRecursively) {
                var subRecipe = this.recipeDataService.GetPrimaryRecipe(ingredient.ItemId);
                if (subRecipe != null) {
                    evaluation.IsCraftedRecursively = true;
                    evaluation.SubComponents = await this.EvaluateComponentsAsync(subRecipe.Ingredients, userConfig, worldId);

                    uint subTotal = 0;
                    uint subTargetTotal = 0;

                    foreach (var sub in evaluation.SubComponents) {
                        subTotal += sub.TotalCost;
                        subTargetTotal += sub.TotalTargetCost;
                    }

                    uint yieldQty = Math.Max(1u, subRecipe.ResultQuantity);
                    evaluation.UnitCost = (uint)Math.Ceiling((double)subTotal / yieldQty);
                    evaluation.TargetUnitCost = (uint)Math.Ceiling((double)subTargetTotal / yieldQty);
                }
                else {
                    evaluation.UnitCost = await this.GetMarketPriceAsync(ingredient.ItemId, worldId);
                }
            }
            else {
                evaluation.UnitCost = await this.GetMarketPriceAsync(ingredient.ItemId, worldId);
            }

            evaluations.Add(evaluation);
        }

        return evaluations;
    }

    private async Task<uint> GetMarketPriceAsync(uint itemId, uint worldId) {
        var priceData = await this.priceProvider.GetLowestPriceAsync(itemId, worldId, false);
        return priceData?.Price ?? 0;
    }
}