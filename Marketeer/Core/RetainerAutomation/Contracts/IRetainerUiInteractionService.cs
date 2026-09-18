namespace Marketeer.Core.RetainerAutomation.Contracts;

public interface IRetainerUiInteractionService {
    bool IsAddonReady(string addonName);

    bool IsRetainerAvailable(string retainerName);
    bool SelectRetainer(string retainerName);
    void SelectRetainer(int index);

    bool IsMenuReadyForRetainer(string retainerName);
    bool IsMenuOptionAvailable(string optionText);
    bool SelectMenuOption(string optionText);

    void OpenRetainerMarket();
    bool CloseRetainerMarket();
    bool CloseSelectString();
    bool CloseSalesHistory();

    void OpenComparePrices();
    void SetPriceAndConfirm(uint newPrice);
    void CloseItemSearchResult();

    void SelectItemInSellList(int uiIndex);
    void SelectContextMenuItem(int index);
    void ConfirmPriceUpdate(uint newPrice);
    void ConfirmYesNo();

    int GetContextMenuItemIndex(string localizedText);
    void CloseUnexpectedWindows();
    void SkipDialogue();
}