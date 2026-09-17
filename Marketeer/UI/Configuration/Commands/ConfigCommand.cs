using Marketeer.UI.Command.Contracts;
using Marketeer.UI.Configuration.UI;
using Marketeer.UI.Dashboard.UI;
using Marketeer.UI.Localization.Contracts;
using Marketeer.UI.Shell.Contracts;

namespace Marketeer.UI.Configuration.Commands;

public class ConfigCommand : ICommand {
    private INavigationService navigationService;
    private DashboardWindow dashboardWindow;
    private ConfigMenu configMenu;
    private ILocalizationService localizationService;

    public string CommandTrigger => "config";
    public string Description => this.localizationService.Translate("Command_Config_Description");

    public ConfigCommand(
        INavigationService navigationService,
        DashboardWindow dashboardWindow,
        ConfigMenu configMenu,
        ILocalizationService localizationService) {

        this.navigationService = navigationService;
        this.dashboardWindow = dashboardWindow;
        this.configMenu = configMenu;
        this.localizationService = localizationService;
    }

    public void Execute(string arguments) {
        this.navigationService.NavigateTo(this.configMenu);
        this.dashboardWindow.IsOpen = true;
    }
}