namespace Marketeer.Core.RetainerAutomation.Contracts;

public interface IItemCancelAndSellService {
    void TriggerCancelAndSell(uint itemId);
}