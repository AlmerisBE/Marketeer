using BasePlugin.Features.Command.Contracts;
using Dalamud.Game.Command;
using Dalamud.Plugin.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BasePlugin.Features.Command.Services;

public class CommandDispatcher : IDisposable {
    private ICommandManager commandManager;
    private IEnumerable<ICommand> commands;
    private string mainCommand = "/baseplugin";

    public CommandDispatcher(ICommandManager commandManager, IEnumerable<ICommand> commands) {
        this.commandManager = commandManager;
        this.commands = commands;

        this.commandManager.AddHandler(this.mainCommand, new CommandInfo(this.OnCommand) {
            HelpMessage = "Type '/baseplugin help' for more information."
        });
    }

    private void OnCommand(string command, string arguments) {
        var args = arguments.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var subCommand = args.Length > 0 ? args[0].ToLowerInvariant() : string.Empty;
        var subArguments = args.Length > 1 ? args[1] : string.Empty;

        var targetCommand = this.commands.FirstOrDefault(c => c.CommandTrigger.Equals(subCommand, StringComparison.InvariantCultureIgnoreCase));

        if (targetCommand != null) {
            targetCommand.Execute(subArguments);
        }
    }

    public void Dispose() {
        this.commandManager.RemoveHandler(this.mainCommand);
    }
}