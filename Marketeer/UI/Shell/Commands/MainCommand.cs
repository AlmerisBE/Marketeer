using Marketeer.UI.Shell.Contracts;
using Marketeer.UI.Shell.UI;

namespace Marketeer.UI.Shell.Commands;

public class MainCommand : ICommand {
    private MainWindow window;

    public string CommandTrigger => string.Empty;
    public string Description => "Opens the main Marketeer dashboard.";

    public MainCommand(MainWindow window) {
        this.window = window;
    }

    public void Execute(string arguments) {
        this.window.Toggle();
    }
}