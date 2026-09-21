using Marketeer.UI.Localization.Contracts;
using Marketeer.UI.Shell.Contracts;
using Marketeer.UI.Shell.UI;

namespace Marketeer.UI.Shell.Commands;

public class ConfigCommand : ICommand {
    private MainWindow mainWindow;
    private ILocalizationService localizationService;

    public string CommandTrigger => "config";
    public string Description => this.localizationService.Translate("Command_Config_Description");

    public ConfigCommand(MainWindow mainWindow, ILocalizationService localizationService) {
        this.mainWindow = mainWindow;
        this.localizationService = localizationService;
    }

    public void Execute(string arguments) {
        this.mainWindow.IsOpen = true;
    }
}