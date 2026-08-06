using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;
using Marketeer.API.GameData.Contracts;
using Marketeer.API.GameData.Models;
using System;
using System.Linq;

namespace Marketeer.Core.GameData.Services;

public class RecipeDataService : IRecipeDataService {
    private IDataManager dataManager;

    public RecipeDataService(IDataManager dataManager) {
        this.dataManager = dataManager;
    }

    public bool IsCraftable(uint itemId) {
        var sheet = this.dataManager.GetExcelSheet<Recipe>();
        return sheet != null && sheet.Any(r => r.ItemResult.RowId == itemId);
    }

    public RecipeInfo? GetPrimaryRecipe(uint itemId) {
        var sheet = this.dataManager.GetExcelSheet<Recipe>();
        if (sheet == null) {
            return null;
        }

        var recipe = sheet.FirstOrDefault(r => r.ItemResult.RowId == itemId);
        if (recipe.RowId == 0 && recipe.ItemResult.RowId != itemId) {
            return null;
        }

        var info = new RecipeInfo {
            RecipeId = recipe.RowId,
            ResultItemId = itemId,
            ResultQuantity = recipe.AmountResult
        };

        // Dynamically evaluate ingredient array bounds from Lumina sheet
        int maxIngredients = Math.Min(recipe.Ingredient.Count, recipe.AmountIngredient.Count);
        for (int i = 0; i < maxIngredients; i++) {
            var ingredientRowId = recipe.Ingredient[i].RowId;
            var amount = recipe.AmountIngredient[i];

            if (ingredientRowId > 0 && amount > 0) {
                info.Ingredients.Add(new RecipeIngredient {
                    ItemId = ingredientRowId,
                    Quantity = amount
                });
            }
        }

        return info;
    }
}