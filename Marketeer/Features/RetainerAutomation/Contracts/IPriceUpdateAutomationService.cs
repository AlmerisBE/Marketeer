namespace Marketeer.Features.RetainerAutomation.Contracts;

public interface IPriceUpdateAutomationService {
    bool IsUpdating { get; }
    void TriggerPriceUpdate();
    void AbortUpdate();
}