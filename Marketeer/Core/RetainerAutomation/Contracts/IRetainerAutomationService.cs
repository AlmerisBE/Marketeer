namespace Marketeer.Core.RetainerAutomation.Contracts;

public interface IRetainerAutomationService {
    bool IsScanning { get; }
    void TriggerScan();
    void AbortScan();
}