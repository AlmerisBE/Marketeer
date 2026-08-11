namespace Marketeer.API.RetainerAutomation.Contracts;

public interface ICancelListingsAutomationService {
    bool IsCancelling { get; }
    void TriggerCancellation();
    void TriggerSingleItemCancellation(uint itemId, uint? price = null, uint? quantity = null);
    void AbortCancellation();
}