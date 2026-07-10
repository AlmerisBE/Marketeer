using Marketeer.Features.Greeting.Services;
using Marketeer.Features.Localization.Contracts;
using Marketeer.Features.Logging.Contracts;
using Dalamud.Plugin.Services;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Features.Greeting.Services;

public class GreetingServiceTests {

    [Fact]
    public void GreetingService_SayHello_PrintsToChat() {
        // Arrange
        var mockChatGui = Substitute.For<IChatGui>();
        var mockLocalizationService = Substitute.For<ILocalizationService>();
        var mockLogger = Substitute.For<ILoggerService>();

        // On instruit le mock : s'il reçoit "Greeting_Message", il DOIT renvoyer cette phrase.
        mockLocalizationService.Translate("Greeting_Message").Returns("Hello World from Marketeer!");

        var greetingService = new GreetingService(mockChatGui, mockLocalizationService, mockLogger);

        // Act
        greetingService.SayHello();

        // Assert
        // Le test passera au vert car le chat recevra exactement ce que le mock de traduction a fourni.
        mockChatGui.Received(1).Print("Hello World from Marketeer!");
    }
}