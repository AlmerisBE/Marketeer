using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;
using Marketeer.API.GameData.Contracts;
using Marketeer.API.GameData.Models;
using System;
using System.Collections.Generic;

namespace Marketeer.API.GameData.Services;

public class RecipeDataService : IRecipeDataService {
    private IDataManager dataManager;
    private Dictionary<uint, RecipeInfo> recipeIndex = new();

    public RecipeDataService(IDataManager dataManager) {
        this.dataManager = dataManager;
        this.BuildIndex();
    }

    private void BuildIndex() {
        var sheet = this.dataManager.GetExcelSheet<Recipe>();
        if (sheet == null) {
            return;
        }

        foreach (var recipe in sheet) {
            uint resultId = recipe.ItemResult.RowId;
            if (resultId == 0) {
                continue;
            }

            // We index only the primary/first recipe found for an item to avoid duplicates
            if (this.recipeIndex.ContainsKey(resultId)) {
                continue;
            }

            var info = new RecipeInfo {
                RecipeId = recipe.RowId,
                ResultItemId = resultId,
                ResultQuantity = recipe.AmountResult
            };

            int maxIngredients = Math.Min(recipe.Ingredient.Count, recipe.AmountIngredient.Count);
            for (int i = 0; i < maxIngredients; i++) {
                uint ingredientId = recipe.Ingredient[i].RowId;
                byte amount = recipe.AmountIngredient[i];

                if (ingredientId > 0 && amount > 0) {
                    info.Ingredients.Add(new RecipeIngredient {
                        ItemId = ingredientId,
                        Quantity = amount
                    });
                }
            }

            this.recipeIndex[resultId] = info;
        }
    }

    public bool IsCraftable(uint itemId) {
        return this.recipeIndex.ContainsKey(itemId);
    }

    public RecipeInfo? GetPrimaryRecipe(uint itemId) {
        if (this.recipeIndex.TryGetValue(itemId, out var recipe)) {
            return recipe;
        }
        return null;
    }
}