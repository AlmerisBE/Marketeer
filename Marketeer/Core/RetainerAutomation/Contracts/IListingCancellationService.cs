namespace Marketeer.Core.RetainerAutomation.Contracts;

public interface IListingCancellationService {
    bool IsActive { get; }
    void TriggerCancellation(uint itemId);
}