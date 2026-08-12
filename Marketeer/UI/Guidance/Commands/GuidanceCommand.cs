using Marketeer.API.Command.Contracts;
using Marketeer.API.Localization.Contracts;
using Marketeer.UI.Guidance.UI;

namespace Marketeer.UI.Guidance.Commands;

public class GuidanceCommand : ICommand {
    private readonly MarketeerGuideWindow guideWindow;
    private readonly ILocalizationService localizationService;

    public string CommandTrigger => "guide";
    public string Description => this.localizationService.Translate("Command_Guide_Description");

    public GuidanceCommand(MarketeerGuideWindow guideWindow, ILocalizationService localizationService) {
        this.guideWindow = guideWindow;
        this.localizationService = localizationService;
    }

    public void Execute(string arguments) {
        this.guideWindow.IsOpen = !this.guideWindow.IsOpen;
    }
}