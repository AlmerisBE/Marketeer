using Dalamud.Plugin.Services;
using Marketeer.API.Command.Contracts;
using Marketeer.API.RetainerAutomation.Contracts;

namespace Marketeer.UI.RetainerAutomation.Commands;

public class RetainerMenuCommand : ICommand {
    private IRetainerUiInteractionService uiInteractionService;
    private IChatGui chatGui;

    public string CommandTrigger => "retainermenu";
    public string Description => "Selects an option in the retainer menu. Usage: /marketeer retainermenu <option text>";

    public RetainerMenuCommand(IRetainerUiInteractionService uiInteractionService, IChatGui chatGui) {
        this.uiInteractionService = uiInteractionService;
        this.chatGui = chatGui;
    }

    public void Execute(string arguments) {
        if (string.IsNullOrWhiteSpace(arguments)) {
            this.chatGui.PrintError("[Marketeer] Please provide a menu option text to select.");
            return;
        }

        var success = this.uiInteractionService.SelectMenuOption(arguments);

        if (success) {
            this.chatGui.Print($"[Marketeer] Selected menu option containing: {arguments}");
        }
        else {
            this.chatGui.PrintError($"[Marketeer] Could not select menu option '{arguments}'. Ensure the Retainer menu is open.");
        }
    }
}