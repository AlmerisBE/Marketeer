namespace Marketeer.API.Command.Contracts;

public interface ICommand {
    string CommandTrigger { get; }
    string Description { get; }
    void Execute(string arguments);
}