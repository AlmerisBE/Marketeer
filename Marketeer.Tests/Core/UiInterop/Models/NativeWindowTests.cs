using Dalamud.Plugin.Services;
using Marketeer.Core.Logging.Contracts;
using Marketeer.UI.UiInterop.Contracts;
using Marketeer.UI.UiInterop.Models;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Core.UiInterop.Models;

public class NativeWindowTests {
    [Fact]
    public void Close_WithHierarchy_ClosesParentWindowFirst() {
        // Arrange
        var mockGameGui = Substitute.For<IGameGui>();
        var mockWindowService = Substitute.For<INativeWindowService>();
        var mockHierarchyProvider = Substitute.For<IWindowHierarchyProvider>();
        var mockLogger = Substitute.For<ILoggerService>();

        var mockParentWindow = Substitute.For<INativeWindow>();
        mockParentWindow.IsVisible.Returns(true);

        mockHierarchyProvider.GetParentWindowName("SocialList").Returns("Social");
        mockWindowService.GetWindow("Social").Returns(mockParentWindow);

        var childWindow = new NativeWindow(
            "SocialList",
            WindowType.System,
            mockGameGui,
            mockWindowService,
            mockHierarchyProvider,
            mockLogger);

        // Act
        childWindow.Close();

        // Assert
        mockParentWindow.Received(1).Close();
    }
}