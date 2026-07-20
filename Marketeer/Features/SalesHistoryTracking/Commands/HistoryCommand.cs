using Dalamud.Plugin.Services;
using Marketeer.Features.Command.Contracts;
using Marketeer.Features.SalesHistoryTracking.Contracts;
using System;

namespace Marketeer.Features.SalesHistoryTracking.Commands;

public class HistoryCommand : ICommand {
    private ISalesRepository salesRepository;
    private IChatGui chatGui;

    public string CommandTrigger => "history";
    public string Description => "Manage sales history. Usage: /marketeer history clear";

    public HistoryCommand(ISalesRepository salesRepository, IChatGui chatGui) {
        this.salesRepository = salesRepository;
        this.chatGui = chatGui;
    }

    public void Execute(string arguments) {
        if (arguments.Trim().Equals("clear", StringComparison.OrdinalIgnoreCase)) {
            this.salesRepository.ClearSales();
            this.chatGui.Print("[Marketeer] Sales history has been successfully cleared.");
        }
        else {
            this.chatGui.PrintError("[Marketeer] Invalid argument. Usage: /marketeer history clear");
        }
    }
}