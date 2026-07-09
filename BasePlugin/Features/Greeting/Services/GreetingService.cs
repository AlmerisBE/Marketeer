using BasePlugin.Features.Greeting.Contracts;
using BasePlugin.Features.Localization.Contracts;
using BasePlugin.Features.Logging.Contracts;
using Dalamud.Plugin.Services;

namespace BasePlugin.Features.Greeting.Services;

public class GreetingService : IGreetingService {
    private IChatGui chatGui;
    private ILocalizationService localizationService;
    private ILoggerService logger;

    public GreetingService(IChatGui chatGui, ILocalizationService localizationService, ILoggerService logger) {
        this.chatGui = chatGui;
        this.localizationService = localizationService;
        this.logger = logger;
    }

    public void SayHello() {
        this.logger.Debug("SayHello command triggered by user.");

        var message = this.localizationService.Translate("Greeting_Message");
        this.chatGui.Print(message);

        this.logger.Info("Greeting message successfully printed to chat.");
    }
}