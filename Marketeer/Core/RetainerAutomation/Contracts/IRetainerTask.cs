namespace Marketeer.Core.RetainerAutomation.Contracts;

public interface IRetainerTask {
    void OnMenuOpened(string retainerName);

    // Called every framework tick while the menu is open. 
    // Returns true when the task is completely finished with this retainer.
    bool OnTick();

    void OnMenuClosed(string retainerName);
    void OnAbort();
}