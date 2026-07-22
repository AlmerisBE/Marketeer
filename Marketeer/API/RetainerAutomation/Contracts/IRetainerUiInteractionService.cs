namespace Marketeer.API.RetainerAutomation.Contracts;

public interface IRetainerUiInteractionService {
    bool IsAddonReady(string addonName);

    // Retainer selection
    bool IsRetainerAvailable(string retainerName);
    bool SelectRetainer(string retainerName);
    void SelectRetainer(int index);

    // Retainer menu manipulation
    bool IsMenuReadyForRetainer(string retainerName);
    bool IsMenuOptionAvailable(string optionText);
    bool SelectMenuOption(string optionText);

    // Direct UI callbacks
    void OpenRetainerMarket();
    bool CloseRetainerMarket();
    bool CloseSelectString();
    bool CloseSalesHistory();

    void SelectItemInSellList(int uiIndex);
    void SelectContextMenuItem(int index);
    void ConfirmPriceUpdate(uint newPrice);
    void ConfirmYesNo();
}