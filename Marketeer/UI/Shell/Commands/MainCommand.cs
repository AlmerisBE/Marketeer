using Marketeer.UI.Command.Contracts;
using Marketeer.UI.Dashboard.UI;

namespace Marketeer.UI.Shell.Commands;

public class MainCommand : ICommand {
    private DashboardWindow window;

    public string CommandTrigger => string.Empty;
    public string Description => "Opens the main Marketeer dashboard.";

    public MainCommand(DashboardWindow window) {
        this.window = window;
    }

    public void Execute(string arguments) {
        this.window.Toggle();
    }
}