using Dalamud.Plugin.Services;
using Marketeer.Features.Command.Contracts;
using Marketeer.Features.Retainers.Contracts;

namespace Marketeer.Features.Retainers.Commands;

public class RetainerMenuCommandAction : ICommand {
    private IRetainerService retainerService;
    private IChatGui chatGui;

    public string CommandTrigger => "retainermenu";
    public string Description => "Selects an option in the retainer menu. Usage: /marketeer retainermenu <option text>";

    public RetainerMenuCommandAction(IRetainerService retainerService, IChatGui chatGui) {
        this.retainerService = retainerService;
        this.chatGui = chatGui;
    }

    public void Execute(string arguments) {
        if (string.IsNullOrWhiteSpace(arguments)) {
            this.chatGui.PrintError("[Marketeer] Please provide a menu option text to select.");
            return;
        }

        var success = this.retainerService.SelectMenuOption(arguments);

        if (success) {
            this.chatGui.Print($"[Marketeer] Selected menu option containing: {arguments}");
        }
        else {
            this.chatGui.PrintError($"[Marketeer] Could not select menu option '{arguments}'. Ensure the Retainer menu is open.");
        }
    }
}