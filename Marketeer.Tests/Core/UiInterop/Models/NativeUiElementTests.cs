using Dalamud.Plugin.Services;
using Marketeer.API.UiInterop.Contracts;
using Marketeer.API.UiInterop.Models;
using Marketeer.Core.Logging.Contracts;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Core.UiInterop.Models;

public class NativeUiElementTests {
    [Fact]
    public void Click_WhenElementIsText_LogsWarningAndDoesNotInteract() {
        // Arrange
        var mockGameGui = Substitute.For<IGameGui>();
        var mockLogger = Substitute.For<ILoggerService>();

        var element = new NativeUiElement("Just Text", NativeUiElementType.Text, 10, "TestAddon", mockGameGui, mockLogger);

        // Act
        element.Click();

        // Assert
        mockLogger.Received(1).Warning(Arg.Is<string>(s => s.Contains("non-clickable element")));
        mockGameGui.DidNotReceive().GetAddonByName(Arg.Any<string>());
    }

    [Fact]
    public void Click_WhenElementIsButton_AttemptsToFindAddon() {
        // Arrange
        var mockGameGui = Substitute.For<IGameGui>();
        var mockLogger = Substitute.For<ILoggerService>();

        // We do not mock GetAddonByName explicitly. 
        // NSubstitute automatically returns the default struct wrapper (which resolves to IntPtr.Zero), safely fulfilling our test logic.
        var element = new NativeUiElement("Confirm", NativeUiElementType.Button, 15, "TestAddon", mockGameGui, mockLogger);

        // Act
        element.Click();

        // Assert
        mockGameGui.Received(1).GetAddonByName("TestAddon");
        mockLogger.Received(1).Error(Arg.Is<string>(s => s.Contains("not found for click action")));
    }
}