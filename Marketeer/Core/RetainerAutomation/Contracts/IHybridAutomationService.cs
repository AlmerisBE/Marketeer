namespace Marketeer.Core.RetainerAutomation.Contracts;

public interface IHybridAutomationService {
    bool IsActive { get; }
    void TriggerAdjustment();
}