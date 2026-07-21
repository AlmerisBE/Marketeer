using Dalamud.Plugin.Services;
using Marketeer.API.Logging.Contracts;
using Marketeer.API.UiInterop.Contracts;
using Marketeer.Core.UiInterop.Services;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Features.WindowAbstraction.Services;

public class NativeWindowServiceTests {
    [Fact]
    public void NativeWindowService_GetWindow_ReturnsCorrectWindowInstance() {
        // Arrange
        var mockGameGui = Substitute.For<IGameGui>();
        var mockHierarchyProvider = Substitute.For<IWindowHierarchyProvider>();
        var mockLogger = Substitute.For<ILoggerService>();

        var service = new NativeWindowService(mockGameGui, mockHierarchyProvider, mockLogger);

        // Act
        var result = service.GetWindow("InventoryLarge");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("InventoryLarge", result!.Name);
        Assert.Equal(WindowType.Inventory, result.Type);
    }

    [Fact]
    public void NativeWindowService_GetWindowType_MapsCorrectly() {
        // Arrange
        var mockGameGui = Substitute.For<IGameGui>();
        var mockHierarchyProvider = Substitute.For<IWindowHierarchyProvider>();
        var mockLogger = Substitute.For<ILoggerService>();

        var service = new NativeWindowService(mockGameGui, mockHierarchyProvider, mockLogger);

        // Act
        var inventory = service.GetWindow("Inventory");
        var dialog = service.GetWindow("SelectString");
        var menu = service.GetWindow("ContextMenu");
        var unknown = service.GetWindow("SomeRandomAddon");

        // Assert
        Assert.Equal(WindowType.Inventory, inventory!.Type);
        Assert.Equal(WindowType.Dialog, dialog!.Type);
        Assert.Equal(WindowType.Menu, menu!.Type);
        Assert.Equal(WindowType.Unknown, unknown!.Type);
    }
}