using Marketeer.UI.Command.Contracts;
using Marketeer.UI.Localization.Contracts;
using Marketeer.UI.RetainerOverlays.UI;

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