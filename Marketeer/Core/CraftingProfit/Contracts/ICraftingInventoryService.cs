namespace Marketeer.Core.CraftingProfit.Contracts;

public interface ICraftingInventoryService {
    uint GetTotalOwnedQuantity(uint itemId);
}