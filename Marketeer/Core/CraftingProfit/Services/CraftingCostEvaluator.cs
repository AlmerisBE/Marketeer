using Marketeer.API.GameData.Contracts;
using Marketeer.API.GameData.Models;
using Marketeer.Core.CraftingProfit.Contracts;
using Marketeer.Core.CraftingProfit.Models;
using Marketeer.Core.Logging.Contracts;
using Marketeer.Core.MarketPricing.Contracts;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Marketeer.Core.CraftingProfit.Services;

public class CraftingCostEvaluator : ICraftingCostEvaluator {
    private IMarketPriceCacheService priceProvider;
    private IRecipeDataService recipeDataService;
    private ICraftingInventoryService inventoryService;
    private ILoggerService logger;

    public CraftingCostEvaluator(
        IMarketPriceCacheService priceProvider,
        IRecipeDataService recipeDataService,
        ICraftingInventoryService inventoryService,
        ILoggerService logger) {

        this.priceProvider = priceProvider;
        this.recipeDataService = recipeDataService;
        this.inventoryService = inventoryService;
        this.logger = logger;
    }

    public async Task<CraftingProfitResult> EvaluateAsync(CraftingItemConfig config, uint worldId) {
        var result = new CraftingProfitResult {
            ItemId = config.ItemId,
            TargetSellPrice = config.TargetSellPrice
        };

        try {
            var currentMarketData = await this.priceProvider.GetPricingAsync(config.ItemId, worldId, false);
            var lowestLive = currentMarketData?.Listings.Count > 0 ? currentMarketData.Listings[0].Price : 0;

            result.CurrentMarketPrice = lowestLive;
            result.HistoricalAveragePrice = currentMarketData?.AverageSalePrice ?? 0;
            result.SalesPerDay = currentMarketData?.SalesPerDay ?? 0f;

            var recipe = this.recipeDataService.GetPrimaryRecipe(config.ItemId);
            if (recipe != null) {
                result.ResultQuantity = Math.Max(1u, recipe.ResultQuantity);

                // Use a shared cache of initial inventory values to prevent redundant loops through the snapshots
                var inventoryCache = new Dictionary<uint, uint>();

                result.ComponentEvaluations = await this.EvaluateComponentsAsync(recipe.Ingredients, config.Components, worldId, config.ItemId, inventoryCache);

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

                result.MaxCraftable = this.CalculateMaxCraftable(config.ItemId, config.Components, inventoryCache, config.ItemId);
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
        uint worldId,
        uint rootItemId,
        Dictionary<uint, uint> inventoryCache) {

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
                    evaluation.SubComponents = await this.EvaluateComponentsAsync(subRecipe.Ingredients, userConfig, worldId, rootItemId, inventoryCache);

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
                else evaluation.UnitCost = await this.GetMarketPriceAsync(ingredient.ItemId, worldId);
            }
            else evaluation.UnitCost = await this.GetMarketPriceAsync(ingredient.ItemId, worldId);

            evaluation.MaxCraftable = this.CalculateMaxCraftable(ingredient.ItemId, userConfig, inventoryCache, rootItemId);

            evaluations.Add(evaluation);
        }

        return evaluations;
    }

    private uint CalculateMaxCraftable(uint targetItemId, Dictionary<uint, ComponentConfig> config, Dictionary<uint, uint> inventoryCache, uint rootItemId) {
        var simulatedInventory = new Dictionary<uint, uint>();
        uint count = 0;

        // Failsafe limit of 999999 prevents infinite loops on theoretically free items
        while (this.TryConsume(targetItemId, 1, config, simulatedInventory, inventoryCache, rootItemId) && count < 999999) {
            count++;
        }
        return count;
    }

    private bool TryConsume(uint itemId, uint amount, Dictionary<uint, ComponentConfig> config, Dictionary<uint, uint> inv, Dictionary<uint, uint> invCache, uint rootItemId) {
        if (!inv.ContainsKey(itemId)) inv[itemId] = this.GetInventoryCount(itemId, invCache);

        if (inv[itemId] >= amount) {
            inv[itemId] -= amount;
            return true;
        }

        uint available = inv[itemId];
        uint shortage = amount - available;

        bool isRoot = itemId == rootItemId;
        config.TryGetValue(itemId, out var compConfig);
        bool craftRecursively = isRoot || (compConfig != null && compConfig.CraftRecursively);

        if (!craftRecursively) return false;

        var recipe = this.recipeDataService.GetPrimaryRecipe(itemId);
        if (recipe == null) return false;

        uint yield = Math.Max(1u, recipe.ResultQuantity);
        uint craftsNeeded = (uint)Math.Ceiling((double)shortage / yield);

        // Transactional evaluation: clone the inventory state so we can back out if an ingredient is missing midway
        var transactionInv = new Dictionary<uint, uint>(inv);

        foreach (var ingredient in recipe.Ingredients) {
            if (!this.TryConsume(ingredient.ItemId, ingredient.Quantity * craftsNeeded, config, transactionInv, invCache, rootItemId)) {
                return false;
            }
        }

        // Commit transaction upon success
        foreach (var kvp in transactionInv) inv[kvp.Key] = kvp.Value;

        inv[itemId] += craftsNeeded * yield;
        inv[itemId] -= amount;

        return true;
    }

    private uint GetInventoryCount(uint itemId, Dictionary<uint, uint> cache) {
        if (!cache.TryGetValue(itemId, out var count)) {
            count = this.inventoryService.GetTotalOwnedQuantity(itemId);
            cache[itemId] = count;
        }
        return count;
    }

    private async Task<uint> GetMarketPriceAsync(uint itemId, uint worldId) {
        var priceData = await this.priceProvider.GetPricingAsync(itemId, worldId, false);
        return priceData?.Listings.Count > 0 ? priceData.Listings[0].Price : 0;
    }
}