namespace Marketeer.API.CraftingProfit.Contracts;

public interface IItemActionProvider {
    bool CanOpenRecipe(uint itemId);
    void OpenRecipe(uint itemId);
    bool CanOpenGatheringMap(uint itemId);
    void OpenGatheringMap(uint itemId);
}