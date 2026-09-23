using System.Collections.Generic;

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
    void OpenComparePrices(nint addonAddress = 0);
    void SetPriceAndConfirm(uint newPrice);
    void CloseItemSearchResult();
    void OpenContextMenuForSellList(int uiIndex);
    void SelectContextMenuItem(int index);
    void ConfirmPriceUpdate(uint newPrice);
    void ConfirmYesNo();
    int GetContextMenuItemIndex(string localizedText);
    int GetContextMenuItemIndex(IEnumerable<string> localizedTexts);
    void CloseUnexpectedWindows();
    void SkipDialogue();
    bool GetActiveRetainerSellItemData(out List<string> windowTexts, out uint currentPrice);
    string? GetRetainerSellListItemName(int index);
}