using Dalamud.Plugin.Services;
using Marketeer.Core.Logging.Contracts;
using Marketeer.UI.UiInterop.Commands;
using Marketeer.UI.UiInterop.Contracts;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.UI.UiInterop.Commands;

public class NativeDevCommandTests {
    [Fact]
    public void Execute_WithDumpAction_LogsOpenWindows() {
        var windowService = Substitute.For<INativeWindowService>();
        var chatGui = Substitute.For<IChatGui>();
        var logger = Substitute.For<ILoggerService>();

        var mockWindow = Substitute.For<INativeWindow>();
        mockWindow.Name.Returns("TestWindow");
        mockWindow.Type.Returns(WindowType.System);

        windowService.GetOpenWindows().Returns(new List<INativeWindow> { mockWindow });

        var command = new NativeDevCommand(windowService, chatGui, logger);

        command.Execute("dump");

        logger.Received().Debug(Arg.Is<string>(s => s.Contains("- TestWindow")));
        chatGui.Received().Print(Arg.Is<string>(s => s.Contains("Dumped")));
    }

    [Fact]
    public void Execute_WithElementsAction_LogsFocusedWindowElements() {
        var windowService = Substitute.For<INativeWindowService>();
        var chatGui = Substitute.For<IChatGui>();
        var logger = Substitute.For<ILoggerService>();

        var mockWindow = Substitute.For<INativeWindow>();
        mockWindow.Name.Returns("FocusedWindow");

        var mockElement = Substitute.For<INativeUiElement>();
        mockElement.Text.Returns("Click Me");
        mockElement.Type.Returns(NativeUiElementType.Button);
        mockElement.NodeId.Returns(42u);

        mockWindow.GetElements().Returns(new List<INativeUiElement> { mockElement });
        windowService.GetFocusedWindow().Returns(mockWindow);

        var command = new NativeDevCommand(windowService, chatGui, logger);

        command.Execute("elements");

        logger.Received().Debug(Arg.Is<string>(s => s.Contains("[Button] NodeID: 42 | Text: \"Click Me\"")));
        chatGui.Received().Print(Arg.Is<string>(s => s.Contains("Dumped")));
    }
}