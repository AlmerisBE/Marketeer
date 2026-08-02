using Dalamud.Game.Gui.ContextMenu;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using Marketeer.API.GameInterop.Contracts;
using Marketeer.API.GameInterop.Models;
using Marketeer.API.Localization.Contracts;
using Marketeer.API.Logging.Contracts;
using Marketeer.API.RetainerAutomation.Contracts;
using Marketeer.API.UiInterop.Contracts;
using Marketeer.Core.RetainerAutomation.Services;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Core.RetainerAutomation.Services;

public class TestableRetainerContextMenuService : RetainerContextMenuService {
    public int MockedTargetIndex { get; set; } = 0;

    public TestableRetainerContextMenuService(
        IContextMenu contextMenu,
        IPriceUpdateAutomationService priceUpdateService,
        IItemCancelAndSellService itemCancelAndSellService,
        ILocalizationService localization,
        INativeWindowService windowService,
        IInventoryService inventoryService,
        ILoggerService logger)
        : base(contextMenu, priceUpdateService, itemCancelAndSellService, localization, windowService, inventoryService, logger) { }

    protected override int GetTargetIndex(IMenuOpenedArgs args) {
        return this.MockedTargetIndex;
    }
}

public class RetainerContextMenuServiceTests {
    [Fact]
    public void OnMenuOpened_WithRetainerSellList_ShouldAlwaysAddMenuItems() {
        var mockContextMenu = Substitute.For<IContextMenu>();
        var mockPriceService = Substitute.For<IPriceUpdateAutomationService>();
        var mockItemCancelAndSellService = Substitute.For<IItemCancelAndSellService>();
        var mockLocalization = Substitute.For<ILocalizationService>();
        var mockWindowService = Substitute.For<INativeWindowService>();
        var mockInventoryService = Substitute.For<IInventoryService>();
        var mockLogger = Substitute.For<ILoggerService>();

        mockLocalization.Translate(Arg.Any<string>()).Returns("Mock String");

        var mockWindow = Substitute.For<INativeWindow>();
        mockWindow.IsVisible.Returns(true);
        mockWindowService.GetWindow("RetainerSellList").Returns(mockWindow);

        using var service = new TestableRetainerContextMenuService(
            mockContextMenu, mockPriceService, mockItemCancelAndSellService, mockLocalization, mockWindowService, mockInventoryService, mockLogger);

        var mockArgs = Substitute.For<IMenuOpenedArgs>();
        mockArgs.AddonName.Returns("RetainerSellList");
        mockArgs.Target.Returns((MenuTarget)null!);

        mockContextMenu.OnMenuOpened += Raise.Event<IContextMenu.OnMenuOpenedDelegate>(mockArgs);

        mockArgs.Received(2).AddMenuItem(Arg.Any<MenuItem>());
    }

    [Fact]
    public void OnMenuItemClicked_ShouldResolveItemIdAndTriggerService() {
        var mockContextMenu = Substitute.For<IContextMenu>();
        var mockPriceService = Substitute.For<IPriceUpdateAutomationService>();
        var mockItemCancelAndSellService = Substitute.For<IItemCancelAndSellService>();
        var mockLocalization = Substitute.For<ILocalizationService>();
        var mockWindowService = Substitute.For<INativeWindowService>();
        var mockInventoryService = Substitute.For<IInventoryService>();
        var mockLogger = Substitute.For<ILoggerService>();

        mockLocalization.Translate("ContextMenu_CancelAndSell").Returns("Cancel and Sell");

        var mockWindow = Substitute.For<INativeWindow>();
        mockWindow.IsVisible.Returns(true);
        mockWindowService.GetWindow("RetainerSellList").Returns(mockWindow);

        mockInventoryService.GetInventorySlots(InventoryType.RetainerMarket)
            .Returns(new List<InventorySlotInfo> {
                new InventorySlotInfo { SlotIndex = 0, IsOccupied = false, ItemId = 0 },
                new InventorySlotInfo { SlotIndex = 1, IsOccupied = true, ItemId = 4242 }
            });

        using var service = new TestableRetainerContextMenuService(
            mockContextMenu, mockPriceService, mockItemCancelAndSellService, mockLocalization, mockWindowService, mockInventoryService, mockLogger);

        service.MockedTargetIndex = 0;

        var addedMenuItems = new List<MenuItem>();
        var mockArgs = Substitute.For<IMenuOpenedArgs>();
        mockArgs.AddonName.Returns("RetainerSellList");
        mockArgs.Target.Returns((MenuTarget)null!);

        mockArgs.When(x => x.AddMenuItem(Arg.Any<MenuItem>()))
                .Do(info => addedMenuItems.Add(info.Arg<MenuItem>()));

        mockContextMenu.OnMenuOpened += Raise.Event<IContextMenu.OnMenuOpenedDelegate>(mockArgs);

        var cancelOption = addedMenuItems.FirstOrDefault(m => m.Name.TextValue == "Cancel and Sell");

        Assert.NotNull(cancelOption);
        Assert.NotNull(cancelOption.OnClicked);

        var clickArgs = Substitute.For<IMenuItemClickedArgs>();
        cancelOption!.OnClicked!.Invoke(clickArgs);

        mockItemCancelAndSellService.Received(1).TriggerCancelAndSell(4242);
    }
}