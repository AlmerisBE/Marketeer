using Marketeer.API.CraftingProfit.Contracts;
using Marketeer.API.CraftingProfit.Models;
using Marketeer.API.GameData.Contracts;
using Marketeer.API.GameData.Models; // <-- Ajout de la directive manquante
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
                result.ComponentEvaluations = await this.EvaluateComponentsAsync(recipe.Ingredients, config.Components, worldId);

                uint totalCost = 0;
                foreach (var eval in result.ComponentEvaluations) {
                    totalCost += eval.TotalCost;
                }

                result.TotalCraftingCost = (uint)Math.Ceiling((double)totalCost / recipe.ResultQuantity);
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
            bool craftRecursively = compConfig != null && compConfig.CraftRecursively;

            if (craftRecursively) {
                var subRecipe = this.recipeDataService.GetPrimaryRecipe(ingredient.ItemId);
                if (subRecipe != null) {
                    evaluation.IsCraftedRecursively = true;
                    evaluation.SubComponents = await this.EvaluateComponentsAsync(subRecipe.Ingredients, userConfig, worldId);

                    uint subTotal = 0;
                    foreach (var sub in evaluation.SubComponents) {
                        subTotal += sub.TotalCost;
                    }
                    evaluation.UnitCost = (uint)Math.Ceiling((double)subTotal / subRecipe.ResultQuantity);
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