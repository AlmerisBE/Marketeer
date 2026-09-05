namespace Marketeer.Core.RetainerAutomation.Contracts;

public interface IRetainerSwitcherService {
    void SwitchTo(string retainerName, bool openMarketList = false);
}