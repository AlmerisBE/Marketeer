using Dalamud.Game.Gui.ContextMenu;
using Dalamud.Plugin.Services;
using Marketeer.API.UiInterop.Contracts;
using Marketeer.Core.Logging.Contracts;
using Marketeer.UI.Localization.Contracts;
using Marketeer.UI.RetainerOverlays.Services;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Marketeer.Tests.UI.RetainerOverlays.Services;

public class RetainerContextMenuServiceTests {
    private IContextMenu contextMenu;
    private ILocalizationService localization;
    private INativeWindowService windowService;
    private ILoggerService logger;

    private IMenuOpenedArgs menuArgs;
    private IContextMenu.OnMenuOpenedDelegate? capturedEventHandler;

    public RetainerContextMenuServiceTests() {
        this.contextMenu = Substitute.For<IContextMenu>();
        this.localization = Substitute.For<ILocalizationService>();
        this.windowService = Substitute.For<INativeWindowService>();
        this.logger = Substitute.For<ILoggerService>();

        this.menuArgs = Substitute.For<IMenuOpenedArgs>();

        this.contextMenu.When(x => x.OnMenuOpened += Arg.Any<IContextMenu.OnMenuOpenedDelegate>())
            .Do(x => this.capturedEventHandler = x.Arg<IContextMenu.OnMenuOpenedDelegate>());
    }

    private class TestableRetainerContextMenuService : RetainerContextMenuService {
        public uint MockTargetItemId { get; set; } = 0;

        public TestableRetainerContextMenuService(
            IContextMenu contextMenu,
            ILocalizationService localization,
            INativeWindowService windowService,
            ILoggerService logger) : base(
                contextMenu, localization, windowService, logger) { }

        protected override uint GetTargetItemIdIfInMarket() {
            return this.MockTargetItemId;
        }
    }

    [Fact]
    public void OnMenuOpened_DoesNotThrow_WhenAddonIsRetainerSellList_AndItemIdIsValid() {
        var service = new TestableRetainerContextMenuService(
            this.contextMenu, this.localization, this.windowService, this.logger);

        service.MockTargetItemId = 12345;

        var mockWindow = Substitute.For<INativeWindow>();
        mockWindow.IsVisible.Returns(true);
        this.windowService.GetWindow("RetainerSellList").Returns(mockWindow);

        var exception = Record.Exception(() => this.capturedEventHandler?.Invoke(this.menuArgs));

        Assert.Null(exception);
        this.windowService.Received(1).GetWindow("RetainerSellList");

        // Assert that the native menu is not altered since automated items were removed
        this.menuArgs.DidNotReceive().AddMenuItem(Arg.Any<MenuItem>());
    }

    [Fact]
    public void OnMenuOpened_ReturnsEarly_WhenAddonIsNotRetainerSellList() {
        var service = new TestableRetainerContextMenuService(
            this.contextMenu, this.localization, this.windowService, this.logger);

        service.MockTargetItemId = 12345;

        var mockWindow = Substitute.For<INativeWindow>();
        mockWindow.IsVisible.Returns(false);
        this.windowService.GetWindow("RetainerSellList").Returns(mockWindow);

        this.capturedEventHandler?.Invoke(this.menuArgs);

        this.menuArgs.DidNotReceive().AddMenuItem(Arg.Any<MenuItem>());
    }

    [Fact]
    public void OnMenuOpened_LogsError_WhenExceptionIsThrown() {
        var service = new TestableRetainerContextMenuService(
            this.contextMenu, this.localization, this.windowService, this.logger);

        this.windowService.GetWindow(Arg.Any<string>()).Throws(new Exception("Test exception"));

        this.capturedEventHandler?.Invoke(this.menuArgs);

        this.logger.Received(1).Error(Arg.Any<Exception>(), "[ContextMenu] Failed to evaluate context menu.");
    }

    [Fact]
    public void Dispose_UnsubscribesFromEvent() {
        var service = new TestableRetainerContextMenuService(
            this.contextMenu, this.localization, this.windowService, this.logger);

        service.Dispose();

        this.contextMenu.Received(1).OnMenuOpened -= Arg.Any<IContextMenu.OnMenuOpenedDelegate>();
    }
}