using Marketeer.Features.Logging.Contracts;
using Marketeer.Features.Retainers.Services;
using Marketeer.Features.WindowAbstraction.Contracts;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Features.Retainers.Services;

public class RetainerServiceTests {

    [Fact]
    public void CloseRetainerMenu_WhenWindowIsVisible_SendsCancelCallbackWithUpdateStateTrue() {
        // Arrange
        var mockWindowService = Substitute.For<INativeWindowService>();
        var mockLogger = Substitute.For<ILoggerService>();
        var mockWindow = Substitute.For<INativeWindow>();

        mockWindow.IsVisible.Returns(true);
        mockWindowService.GetWindow("SelectString").Returns(mockWindow);

        var service = new RetainerService(mockWindowService, mockLogger);

        // Act
        var result = service.CloseRetainerMenu();

        // Assert
        Assert.True(result);

        mockWindow.Received(1).SendCallbackWithUpdateState(true, -1);
    }

    [Fact]
    public void CloseMarketListings_WhenWindowIsVisible_SendsCancelCallbackWithUpdateStateTrue() {
        // Arrange
        var mockWindowService = Substitute.For<INativeWindowService>();
        var mockLogger = Substitute.For<ILoggerService>();
        var mockWindow = Substitute.For<INativeWindow>();

        mockWindow.IsVisible.Returns(true);
        mockWindowService.GetWindow("RetainerSellList").Returns(mockWindow);

        var service = new RetainerService(mockWindowService, mockLogger);

        // Act
        var result = service.CloseMarketListings();

        // Assert
        Assert.True(result);
        mockWindow.Received(1).SendCallbackWithUpdateState(true, -1);
    }

    [Fact]
    public void SelectMenuOption_WhenWindowIsNotVisible_ReturnsFalse() {
        // Arrange
        var mockWindowService = Substitute.For<INativeWindowService>();
        var mockLogger = Substitute.For<ILoggerService>();
        var mockWindow = Substitute.For<INativeWindow>();

        mockWindow.IsVisible.Returns(false);
        mockWindowService.GetWindow("SelectString").Returns(mockWindow);

        var service = new RetainerService(mockWindowService, mockLogger);

        // Act
        var result = service.SelectMenuOption("Sell items");

        // Assert
        Assert.False(result);
        mockLogger.Received(1).Warning(Arg.Is<string>(s => s.Contains("not visible")));
    }

    [Fact]
    public void SelectMenuOption_WhenOptionExistsWithDynamicSuffix_SendsCallbackWithUpdateStateAndDefersToServer() {
        // Arrange
        var mockWindowService = Substitute.For<INativeWindowService>();
        var mockLogger = Substitute.For<ILoggerService>();
        var mockWindow = Substitute.For<INativeWindow>();

        var mockOption0 = Substitute.For<INativeUiElement>();
        mockOption0.Type.Returns(NativeUiElementType.Button);
        mockOption0.Text.Returns("Trade items [Retainer: 172 slots occupied]");

        var mockOption1 = Substitute.For<INativeUiElement>();
        mockOption1.Type.Returns(NativeUiElementType.Button);
        mockOption1.Text.Returns("Sell items in your retainer's inventory");

        mockWindow.IsVisible.Returns(true);
        mockWindow.GetElements().Returns(new List<INativeUiElement> { mockOption0, mockOption1 });
        mockWindowService.GetWindow("SelectString").Returns(mockWindow);

        var service = new RetainerService(mockWindowService, mockLogger);

        // Act
        var result = service.SelectMenuOption("Sell items in your retainer's inventory");

        // Assert
        Assert.True(result);

        // Verify the callback was sent with the UI updateState flag set to TRUE
        mockWindow.Received(1).SendCallbackWithUpdateState(true, Arg.Is<object[]>(args =>
            args.Length == 1 &&
            (int)args[0] == 1));

        // Ensure manual Close() is NOT called, as updateState=true handles it natively
        mockWindow.DidNotReceive().Close();
    }

    [Fact]
    public void SelectMenuOption_WithPartialInnerMatch_ReturnsFalseAndDoesNotSendCallback() {
        // Arrange
        var mockWindowService = Substitute.For<INativeWindowService>();
        var mockLogger = Substitute.For<ILoggerService>();
        var mockWindow = Substitute.For<INativeWindow>();
        var mockElement = Substitute.For<INativeUiElement>();

        mockWindow.IsVisible.Returns(true);
        mockElement.Type.Returns(NativeUiElementType.Button);
        mockElement.Text.Returns("Sell items in your retainer's inventory");

        mockWindow.GetElements().Returns(new List<INativeUiElement> { mockElement });
        mockWindowService.GetWindow("SelectString").Returns(mockWindow);

        var service = new RetainerService(mockWindowService, mockLogger);

        // Act
        var result = service.SelectMenuOption("items in your retainer's");

        // Assert
        Assert.False(result);
        mockWindow.DidNotReceiveWithAnyArgs().SendCallbackWithUpdateState(default, default!);
        mockWindow.DidNotReceive().Close();
    }
}