namespace Marketeer.API.RetainerAutomation.Contracts;

public interface ICancelListingsAutomationService {
    bool IsCancelling { get; }
    void TriggerCancellation();
    void TriggerSingleItemCancellation(uint itemId);
    void AbortCancellation();
}