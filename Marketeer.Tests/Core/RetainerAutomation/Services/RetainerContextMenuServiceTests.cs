using Dalamud.Game.Gui.ContextMenu;
using Dalamud.Plugin.Services;
using Marketeer.Core.Logging.Contracts;
using Marketeer.Core.RetainerAutomation.Contracts;
using Marketeer.UI.Localization.Contracts;
using Marketeer.UI.RetainerOverlays.Services;
using Marketeer.UI.UiInterop.Contracts;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Core.RetainerAutomation.Services;

public class RetainerContextMenuServiceTests {
    private IContextMenu contextMenu;
    private IPriceUpdateAutomationService priceUpdateService;
    private IItemCancelAndSellService itemCancelAndSellService;
    private ILocalizationService localization;
    private INativeWindowService windowService;
    private ILoggerService logger;

    private IMenuOpenedArgs menuArgs;
    private IContextMenu.OnMenuOpenedDelegate? capturedEventHandler;

    public RetainerContextMenuServiceTests() {
        this.contextMenu = Substitute.For<IContextMenu>();
        this.priceUpdateService = Substitute.For<IPriceUpdateAutomationService>();
        this.itemCancelAndSellService = Substitute.For<IItemCancelAndSellService>();
        this.localization = Substitute.For<ILocalizationService>();
        this.windowService = Substitute.For<INativeWindowService>();
        this.logger = Substitute.For<ILoggerService>();

        this.localization.Translate("ContextMenu_Compete").Returns("Compete");
        this.localization.Translate("ContextMenu_CancelAndSell").Returns("Cancel and Sell");

        this.menuArgs = Substitute.For<IMenuOpenedArgs>();

        this.contextMenu.When(x => x.OnMenuOpened += Arg.Any<IContextMenu.OnMenuOpenedDelegate>())
            .Do(x => this.capturedEventHandler = x.Arg<IContextMenu.OnMenuOpenedDelegate>());
    }

    private class TestableRetainerContextMenuService : RetainerContextMenuService {
        public uint MockTargetItemId { get; set; } = 0;

        public TestableRetainerContextMenuService(
            IContextMenu contextMenu,
            IPriceUpdateAutomationService priceUpdateService,
            IItemCancelAndSellService itemCancelAndSellService,
            ILocalizationService localization,
            INativeWindowService windowService,
            ILoggerService logger) : base(
                contextMenu, priceUpdateService, itemCancelAndSellService,
                localization, windowService, logger) { }

        protected override uint GetTargetItemIdIfInMarket() {
            return this.MockTargetItemId;
        }
    }

    [Fact]
    public void OnMenuOpened_AddsMenuItems_WhenAddonIsRetainerSellList_AndItemIdIsValid() {
        var service = new TestableRetainerContextMenuService(
            this.contextMenu, this.priceUpdateService, this.itemCancelAndSellService,
            this.localization, this.windowService, this.logger);

        service.MockTargetItemId = 12345;

        var mockWindow = Substitute.For<INativeWindow>();
        mockWindow.IsVisible.Returns(true);
        this.windowService.GetWindow("RetainerSellList").Returns(mockWindow);

        var addedItems = new List<MenuItem>();
        this.menuArgs.When(x => x.AddMenuItem(Arg.Any<MenuItem>()))
            .Do(x => addedItems.Add(x.Arg<MenuItem>()));

        this.capturedEventHandler?.Invoke(this.menuArgs);

        Assert.Equal(2, addedItems.Count);
        Assert.Contains(addedItems, i => i.Name.TextValue == "Compete");
        Assert.Contains(addedItems, i => i.Name.TextValue == "Cancel and Sell");
    }

    [Fact]
    public void OnMenuOpened_DoesNotAddMenuItems_WhenItemIdIsZero() {
        var service = new TestableRetainerContextMenuService(
            this.contextMenu, this.priceUpdateService, this.itemCancelAndSellService,
            this.localization, this.windowService, this.logger);

        service.MockTargetItemId = 0;

        var mockWindow = Substitute.For<INativeWindow>();
        mockWindow.IsVisible.Returns(true);
        this.windowService.GetWindow("RetainerSellList").Returns(mockWindow);

        this.capturedEventHandler?.Invoke(this.menuArgs);

        this.menuArgs.DidNotReceive().AddMenuItem(Arg.Any<MenuItem>());
    }

    [Fact]
    public void OnMenuOpened_DoesNotAddMenuItems_WhenAddonIsNotRetainerSellList() {
        var service = new TestableRetainerContextMenuService(
            this.contextMenu, this.priceUpdateService, this.itemCancelAndSellService,
            this.localization, this.windowService, this.logger);

        service.MockTargetItemId = 12345;

        var mockWindow = Substitute.For<INativeWindow>();
        mockWindow.IsVisible.Returns(false);
        this.windowService.GetWindow("RetainerSellList").Returns(mockWindow);

        this.capturedEventHandler?.Invoke(this.menuArgs);

        this.menuArgs.DidNotReceive().AddMenuItem(Arg.Any<MenuItem>());
    }

    [Fact]
    public void MenuItems_TriggerCorrectServices_WhenClicked() {
        var service = new TestableRetainerContextMenuService(
            this.contextMenu, this.priceUpdateService, this.itemCancelAndSellService,
            this.localization, this.windowService, this.logger);

        service.MockTargetItemId = 999;

        var mockWindow = Substitute.For<INativeWindow>();
        mockWindow.IsVisible.Returns(true);
        this.windowService.GetWindow("RetainerSellList").Returns(mockWindow);

        var addedItems = new List<MenuItem>();
        this.menuArgs.When(x => x.AddMenuItem(Arg.Any<MenuItem>()))
            .Do(x => addedItems.Add(x.Arg<MenuItem>()));

        this.capturedEventHandler?.Invoke(this.menuArgs);

        var competeItem = addedItems.First(i => i.Name.TextValue == "Compete");
        var cancelItem = addedItems.First(i => i.Name.TextValue == "Cancel and Sell");

        competeItem.OnClicked?.Invoke(Substitute.For<IMenuItemClickedArgs>());
        this.priceUpdateService.Received(1).TriggerSingleItemUpdate(999);

        cancelItem.OnClicked?.Invoke(Substitute.For<IMenuItemClickedArgs>());
        this.itemCancelAndSellService.Received(1).TriggerCancelAndSell(999);
    }
}