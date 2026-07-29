namespace Marketeer.API.RetainerAutomation.Contracts;

public interface IPriceUpdateAutomationService {
    bool IsUpdating { get; }
    void TriggerPriceUpdate();
    void TriggerSingleItemUpdate(object? menuTarget);
    void AbortUpdate();
}