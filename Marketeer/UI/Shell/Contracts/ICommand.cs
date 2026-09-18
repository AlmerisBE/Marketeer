namespace Marketeer.UI.Shell.Contracts;

public interface ICommand {
    string CommandTrigger { get; }
    string Description { get; }
    void Execute(string arguments);
}