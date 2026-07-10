using Marketeer.Features.Command.Contracts;
using Marketeer.Features.Greeting.Contracts;

namespace Marketeer.Features.Greeting.Commands;

public class GreetingCommandAction : ICommand {
    private IGreetingService greetingService;

    public string CommandTrigger => "hello";
    public string Description => "Prints a greeting message to the chat.";

    public GreetingCommandAction(IGreetingService greetingService) {
        this.greetingService = greetingService;
    }

    public void Execute(string arguments) {
        this.greetingService.SayHello();
    }
}