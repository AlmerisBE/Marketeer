namespace Marketeer.API.RetainerAutomation.Contracts;

public interface ICancelAndSellAutomationService {
    bool IsActive { get; }
    void TriggerCancelAndSell(uint itemId, uint quantity, int uiIndex);
}