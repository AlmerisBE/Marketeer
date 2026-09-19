namespace Marketeer.UI.Shell.Contracts;

public interface IToolbarAction {
    string Name { get; }
    string Tooltip { get; }
    int Priority { get; }
    bool IsEnabled { get; }
    void Execute();
}