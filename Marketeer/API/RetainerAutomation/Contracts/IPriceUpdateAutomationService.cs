namespace Marketeer.API.RetainerAutomation.Contracts;

public interface IPriceUpdateAutomationService {
    bool IsUpdating { get; }
    void TriggerPriceUpdate();
    void AbortUpdate();
}