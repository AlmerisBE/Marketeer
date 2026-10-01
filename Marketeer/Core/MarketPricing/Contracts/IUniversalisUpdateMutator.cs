namespace Marketeer.Core.MarketPricing.Contracts;

public interface IUniversalisUpdateMutator {
    void SetUpdating(bool isUpdating);
    void RecordSuccessfulUpdate();
}