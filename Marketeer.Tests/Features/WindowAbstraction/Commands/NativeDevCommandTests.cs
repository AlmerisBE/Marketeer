using Dalamud.Plugin.Services;
using Marketeer.Features.Logging.Contracts;
using Marketeer.Features.WindowAbstraction.Commands;
using Marketeer.Features.WindowAbstraction.Contracts;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Features.WindowAbstraction.Commands;

public class NativeDevCommandTests {
    [Fact]
    public void Execute_WithInvalidArguments_PrintsUsage() {
        // Arrange
        var mockWindowService = Substitute.For<INativeWindowService>();
        var mockChatGui = Substitute.For<IChatGui>();
        var mockLogger = Substitute.For<ILoggerService>();

        var command = new NativeDevCommand(mockWindowService, mockChatGui, mockLogger);

        // Act
        command.Execute("open Something");

        // Assert
        // The expected string must match the exact output defined in NativeDevCommand.cs
        mockChatGui.Received(1).Print("Usage: /marketeer native <close [WindowID] | dump | elements>");
    }

    [Fact]
    public void Execute_WithCloseAction_ClosesWindow() {
        // Arrange
        var mockWindowService = Substitute.For<INativeWindowService>();
        var mockChatGui = Substitute.For<IChatGui>();
        var mockLogger = Substitute.For<ILoggerService>();
        var mockWindow = Substitute.For<INativeWindow>();

        mockWindow.IsVisible.Returns(true);
        mockWindowService.GetWindow("RetainerList").Returns(mockWindow);

        var command = new NativeDevCommand(mockWindowService, mockChatGui, mockLogger);

        // Act
        command.Execute("close RetainerList");

        // Assert
        mockWindow.Received(1).Close();
        mockChatGui.Received(1).Print("[Marketeer] Closed native window: RetainerList");
    }

    [Fact]
    public void Execute_WithDumpAction_LogsOpenWindows() {
        // Arrange
        var mockWindowService = Substitute.For<INativeWindowService>();
        var mockChatGui = Substitute.For<IChatGui>();
        var mockLogger = Substitute.For<ILoggerService>();
        var mockWindow = Substitute.For<INativeWindow>();

        mockWindow.Name.Returns("TestWindow");

        var openWindows = new List<INativeWindow> { mockWindow };
        mockWindowService.GetOpenWindows().Returns(openWindows);

        var command = new NativeDevCommand(mockWindowService, mockChatGui, mockLogger);

        // Act
        command.Execute("dump");

        // Assert
        mockLogger.Received().Info(Arg.Is<string>(s => s.Contains("- TestWindow")));
        mockChatGui.Received(1).Print("[Marketeer] Dumped 1 visible windows to the Dalamud log.");
    }

    [Fact]
    public void Execute_WithElementsAction_LogsFocusedWindowElements() {
        // Arrange
        var mockWindowService = Substitute.For<INativeWindowService>();
        var mockChatGui = Substitute.For<IChatGui>();
        var mockLogger = Substitute.For<ILoggerService>();
        var mockWindow = Substitute.For<INativeWindow>();
        var mockElement = Substitute.For<INativeUiElement>();

        mockWindow.Name.Returns("TestFocusedWindow");
        mockElement.Type.Returns(NativeUiElementType.Button);
        mockElement.Text.Returns("Click Me");
        mockElement.NodeId.Returns(42u);

        mockWindow.GetElements().Returns(new List<INativeUiElement> { mockElement });
        mockWindowService.GetFocusedWindow().Returns(mockWindow);

        var command = new NativeDevCommand(mockWindowService, mockChatGui, mockLogger);

        // Act
        command.Execute("elements");

        // Assert
        mockLogger.Received().Info(Arg.Is<string>(s => s.Contains("[Button] NodeID: 42 | Text: \"Click Me\"")));
        mockChatGui.Received(1).Print("[Marketeer] Dumped 1 elements from 'TestFocusedWindow' to /xllog.");
    }

    [Fact]
    public void Execute_WithElementsAction_WhenNoFocusedWindow_PrintsError() {
        // Arrange
        var mockWindowService = Substitute.For<INativeWindowService>();
        var mockChatGui = Substitute.For<IChatGui>();
        var mockLogger = Substitute.For<ILoggerService>();

        mockWindowService.GetFocusedWindow().Returns((INativeWindow?)null);

        var command = new NativeDevCommand(mockWindowService, mockChatGui, mockLogger);

        // Act
        command.Execute("elements");

        // Assert
        mockLogger.Received(1).Warning(Arg.Is<string>(s => s.Contains("no focused window was found")));
        mockChatGui.Received(1).PrintError("[Marketeer] No focused native window detected.");
    }
}