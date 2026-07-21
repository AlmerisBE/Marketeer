namespace Marketeer.API.RetainerAutomation.Contracts;

public interface IRetainerAutomationService {
    bool IsScanning { get; }
    void TriggerScan();
    void AbortScan();
}