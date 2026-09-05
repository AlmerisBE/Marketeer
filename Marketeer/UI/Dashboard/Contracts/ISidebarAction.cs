namespace Marketeer.UI.Dashboard.Contracts;

public interface ISidebarAction {
    string Name { get; }
    int Priority { get; }
    void Execute();
}