using BasePlugin.Features.Command.Contracts;
using BasePlugin.Features.Command.Services;
using Dalamud.Game.Command;
using Dalamud.Plugin.Services;
using NSubstitute;
using Xunit;

namespace BasePlugin.Tests.Features.Command.Services;

public class CommandDispatcherTests {
    [Fact]
    public void CommandDispatcher_OnInitialization_RegistersMainCommand() {
        // Arrange
        var mockCommandManager = Substitute.For<ICommandManager>();
        var commands = new List<ICommand>();

        // Act
        using var dispatcher = new CommandDispatcher(mockCommandManager, commands);

        // Assert
        mockCommandManager.Received(1).AddHandler("/baseplugin", Arg.Any<CommandInfo>());
    }

    [Fact]
    public void CommandDispatcher_OnCommand_DispatchesToCorrectCommandAction() {
        // Arrange
        var mockCommandManager = Substitute.For<ICommandManager>();

        var mockCommand = Substitute.For<ICommand>();
        mockCommand.CommandTrigger.Returns("hello");

        var commands = new List<ICommand> { mockCommand };
        CommandInfo capturedCommandInfo = null!;

        // Capture the entire CommandInfo object instead of just the delegate
        mockCommandManager.When(x => x.AddHandler(Arg.Any<string>(), Arg.Any<CommandInfo>()))
            .Do(callInfo => capturedCommandInfo = callInfo.Arg<CommandInfo>());

        using var dispatcher = new CommandDispatcher(mockCommandManager, commands);

        // Act
        // Invoke the handler dynamically from the captured object
        capturedCommandInfo.Handler.Invoke("/baseplugin", "hello world");

        // Assert
        mockCommand.Received(1).Execute("world");
    }

    [Fact]
    public void CommandDispatcher_OnCommand_IsCaseInsensitive() {
        // Arrange
        var mockCommandManager = Substitute.For<ICommandManager>();

        var mockCommand = Substitute.For<ICommand>();
        mockCommand.CommandTrigger.Returns("hello");

        var commands = new List<ICommand> { mockCommand };
        CommandInfo capturedCommandInfo = null!;

        mockCommandManager.When(x => x.AddHandler(Arg.Any<string>(), Arg.Any<CommandInfo>()))
            .Do(callInfo => capturedCommandInfo = callInfo.Arg<CommandInfo>());

        using var dispatcher = new CommandDispatcher(mockCommandManager, commands);

        // Act
        capturedCommandInfo.Handler.Invoke("/baseplugin", "HeLlO arGuments");

        // Assert
        mockCommand.Received(1).Execute("arGuments");
    }

    [Fact]
    public void CommandDispatcher_OnDispose_RemovesCommandRegistration() {
        // Arrange
        var mockCommandManager = Substitute.For<ICommandManager>();
        var commands = new List<ICommand>();

        var dispatcher = new CommandDispatcher(mockCommandManager, commands);

        // Act
        dispatcher.Dispose();

        // Assert
        mockCommandManager.Received(1).RemoveHandler("/baseplugin");
    }
}