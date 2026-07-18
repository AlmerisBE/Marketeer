namespace Marketeer.Features.RetainerAutomation.Contracts;

public interface IRetainerAutomationService {
    bool IsScanning { get; }
    void TriggerScan();
    void Reset();
}