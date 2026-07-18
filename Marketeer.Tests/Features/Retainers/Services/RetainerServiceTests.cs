using Marketeer.Features.Logging.Contracts;
using Marketeer.Features.Retainers.Services;
using Marketeer.Features.WindowAbstraction.Contracts;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Features.Retainers.Services;

public class RetainerServiceTests {
    [Fact]
    public void SelectRetainer_WhenWindowIsNotVisible_ReturnsFalse() {
        // Arrange
        var mockWindowService = Substitute.For<INativeWindowService>();
        var mockLogger = Substitute.For<ILoggerService>();
        var mockWindow = Substitute.For<INativeWindow>();

        mockWindow.IsVisible.Returns(false);
        mockWindowService.GetWindow("RetainerList").Returns(mockWindow);

        var service = new RetainerService(mockWindowService, mockLogger);

        // Act
        var result = service.SelectRetainer("Adelaide");

        // Assert
        Assert.False(result);
        mockLogger.Received(1).Warning(Arg.Is<string>(s => s.Contains("not visible")));
    }

    [Fact]
    public void SelectRetainer_WhenRetainerExists_SendsCallbackWithNaturalIndexAndReturnsTrue() {
        // Arrange
        var mockWindowService = Substitute.For<INativeWindowService>();
        var mockLogger = Substitute.For<ILoggerService>();
        var mockWindow = Substitute.For<INativeWindow>();

        var mockRetainerAdelaide = Substitute.For<INativeUiElement>();
        mockRetainerAdelaide.Type.Returns(NativeUiElementType.Button);
        mockRetainerAdelaide.Text.Returns("Fin dans 36m | 19 objets en vente (12) | 233 890 320 | 172 | 100 | Adelaide");

        var mockRetainerTyphene = Substitute.For<INativeUiElement>();
        mockRetainerTyphene.Type.Returns(NativeUiElementType.Button);
        mockRetainerTyphene.Text.Returns("Fin dans 36m | 19 objets en vente (15) | 79 486 | 118 | 100 | Typhene");

        mockWindow.IsVisible.Returns(true);
        mockWindow.GetElements().Returns(new List<INativeUiElement> { mockRetainerAdelaide, mockRetainerTyphene });
        mockWindowService.GetWindow("RetainerList").Returns(mockWindow);

        var service = new RetainerService(mockWindowService, mockLogger);

        // Act
        var result = service.SelectRetainer("Typhene");

        // Assert
        Assert.True(result);
        mockWindow.Received(1).SendCallback(Arg.Is<object[]>(args =>
            args.Length == 2 &&
            (int)args[0] == 2 &&
            (int)args[1] == 1));
    }

    [Fact]
    public void SelectRetainer_WithPartialNameMatch_DoesNotClickAndReturnsFalse() {
        // Arrange
        var mockWindowService = Substitute.For<INativeWindowService>();
        var mockLogger = Substitute.For<ILoggerService>();
        var mockWindow = Substitute.For<INativeWindow>();
        var mockElement = Substitute.For<INativeUiElement>();

        mockWindow.IsVisible.Returns(true);
        mockElement.Type.Returns(NativeUiElementType.Button);
        mockElement.Text.Returns("Fin dans 36m | 100 | 172 | Adelaide");

        mockWindow.GetElements().Returns(new List<INativeUiElement> { mockElement });
        mockWindowService.GetWindow("RetainerList").Returns(mockWindow);

        var service = new RetainerService(mockWindowService, mockLogger);

        // Act - Attempting to select using partial strings
        var resultAde = service.SelectRetainer("Ade");
        var resultA = service.SelectRetainer("A");

        // Assert - Both should fail due to strict Regex matching
        Assert.False(resultAde);
        Assert.False(resultA);
        mockWindow.DidNotReceiveWithAnyArgs().SendCallback(default!);
        mockLogger.Received(2).Warning(Arg.Is<string>(s => s.Contains("not found in the active RetainerList")));
    }
}