namespace Marketeer.API.RetainerAutomation.Contracts;

public interface IItemCancelAndSellService {
    void TriggerCancelAndSell(uint itemId);
}