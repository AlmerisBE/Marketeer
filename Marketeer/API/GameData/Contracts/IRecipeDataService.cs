using Marketeer.API.GameData.Models;

namespace Marketeer.API.GameData.Contracts;

public interface IRecipeDataService {
    RecipeInfo? GetPrimaryRecipe(uint itemId);
    bool IsCraftable(uint itemId);
}