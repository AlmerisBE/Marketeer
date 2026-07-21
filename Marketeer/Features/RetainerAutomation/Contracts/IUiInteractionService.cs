namespace Marketeer.Features.RetainerAutomation.Contracts;

public interface IUiInteractionService {
    bool IsAddonReady(string addonName);
    void SelectRetainer(int index);
    void OpenRetainerMarket();
    void CloseRetainerMarket();
    void CloseSelectString();

    void SelectItemInSellList(int slotIndex);
    void ConfirmPriceUpdate(uint newPrice);
}