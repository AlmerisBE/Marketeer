namespace Marketeer.UI.Shell.Contracts;

public interface ISidebarAction {
    string Name { get; }
    int Priority { get; }
    void Execute();
}