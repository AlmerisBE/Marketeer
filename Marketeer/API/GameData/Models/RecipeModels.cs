namespace Marketeer.API.GameData.Models;

public class RecipeIngredient {
    public uint ItemId { get; set; }
    public uint Quantity { get; set; }
}

public class RecipeInfo {
    public uint RecipeId { get; set; }
    public uint ResultItemId { get; set; }
    public uint ResultQuantity { get; set; }
    public System.Collections.Generic.List<RecipeIngredient> Ingredients { get; set; } = new();
}