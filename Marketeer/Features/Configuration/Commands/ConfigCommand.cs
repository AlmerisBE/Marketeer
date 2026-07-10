using Marketeer.Features.Command.Contracts;
using Marketeer.Features.Configuration.UI;

namespace Marketeer.Features.Configuration.Commands;

public class ConfigCommand : ICommand {
    private ConfigWindow configWindow;

    public string CommandTrigger => "config";
    public string Description => "Ouvre ou ferme la fenêtre de configuration.";

    public ConfigCommand(ConfigWindow configWindow) {
        this.configWindow = configWindow;
    }

    public void Execute(string arguments) {
        this.configWindow.Toggle(); // Provided by Dalamud's Window base class
    }
}