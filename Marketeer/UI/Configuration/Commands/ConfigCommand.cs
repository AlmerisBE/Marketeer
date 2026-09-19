using Marketeer.UI.Configuration.UI;
using Marketeer.UI.Localization.Contracts;
using Marketeer.UI.Shell.Contracts;
using Marketeer.UI.Shell.UI;

namespace Marketeer.UI.Configuration.Commands;

public class ConfigCommand : ICommand {
    private INavigationService navigationService;
    private MainWindow mainWindow;
    private UniversalisConfigMenu defaultMenu;
    private ILocalizationService localizationService;

    public string CommandTrigger => "config";
    public string Description => this.localizationService.Translate("Command_Config_Description");

    public ConfigCommand(
        INavigationService navigationService,
        MainWindow mainWindow,
        UniversalisConfigMenu defaultMenu,
        ILocalizationService localizationService) {

        this.navigationService = navigationService;
        this.mainWindow = mainWindow;
        this.defaultMenu = defaultMenu;
        this.localizationService = localizationService;
    }

    public void Execute(string arguments) {
        this.navigationService.NavigateTo(this.defaultMenu);
        this.mainWindow.IsOpen = true;
    }
}