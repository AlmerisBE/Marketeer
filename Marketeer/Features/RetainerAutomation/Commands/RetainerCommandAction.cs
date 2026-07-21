using Dalamud.Plugin.Services;
using Marketeer.Features.Command.Contracts;
using Marketeer.Features.RetainerAutomation.Contracts;

namespace Marketeer.Features.RetainerAutomation.Commands;

public class RetainerCommandAction : ICommand {
    private IRetainerUiInteractionService retainerService;
    private IChatGui chatGui;

    public string CommandTrigger => "retainer";
    public string Description => "Selects a retainer by name. Usage: /marketeer retainer <name>";

    public RetainerCommandAction(IRetainerUiInteractionService retainerService, IChatGui chatGui) {
        this.retainerService = retainerService;
        this.chatGui = chatGui;
    }

    public void Execute(string arguments) {
        if (string.IsNullOrWhiteSpace(arguments)) {
            this.chatGui.PrintError("[Marketeer] Please provide a retainer name.");
            return;
        }

        var success = this.retainerService.SelectRetainer(arguments);

        if (success) {
            this.chatGui.Print($"[Marketeer] Selected retainer: {arguments}");
        }
        else {
            this.chatGui.PrintError($"[Marketeer] Could not select '{arguments}'. Ensure the Retainer List is open.");
        }
    }
}