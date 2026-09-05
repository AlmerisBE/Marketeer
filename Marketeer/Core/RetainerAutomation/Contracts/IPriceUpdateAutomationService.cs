namespace Marketeer.Core.RetainerAutomation.Contracts;

public interface IPriceUpdateAutomationService {
    bool IsUpdating { get; }
    void TriggerPriceUpdate();
    void TriggerSingleItemUpdate(uint itemId, uint? price = null, uint? quantity = null);
    void AbortUpdate();
}