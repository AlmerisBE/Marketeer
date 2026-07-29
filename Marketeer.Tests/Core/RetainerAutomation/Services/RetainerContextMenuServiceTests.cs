using Dalamud.Game.Gui.ContextMenu;
using Dalamud.Plugin.Services;
using Marketeer.API.Localization.Contracts;
using Marketeer.API.RetainerAutomation.Contracts;
using Marketeer.Core.RetainerAutomation.Services;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Core.RetainerAutomation.Services;

public class RetainerContextMenuServiceTests {
    [Fact]
    public void OnMenuOpened_WithRetainerSellList_ShouldAddMenuItems() {
        var mockContextMenu = Substitute.For<IContextMenu>();
        var mockPriceService = Substitute.For<IPriceUpdateAutomationService>();
        var mockItemCancelAndSellService = Substitute.For<IItemCancelAndSellService>();
        var mockLocalization = Substitute.For<ILocalizationService>();

        mockLocalization.Translate("ContextMenu_Compete").Returns("Compete Price");
        mockLocalization.Translate("ContextMenu_CancelAndSell").Returns("Cancel and Sell");

        using var service = new RetainerContextMenuService(mockContextMenu, mockPriceService, mockItemCancelAndSellService, mockLocalization);

        var mockArgs = Substitute.For<IMenuOpenedArgs>();
        mockArgs.AddonName.Returns("RetainerSellList");

        mockContextMenu.OnMenuOpened += Raise.Event<IContextMenu.OnMenuOpenedDelegate>(mockArgs);

        mockArgs.Received(2).AddMenuItem(Arg.Any<MenuItem>());
    }

    [Fact]
    public void OnMenuOpened_WithOtherAddon_ShouldNotAddItems() {
        var mockContextMenu = Substitute.For<IContextMenu>();
        var mockPriceService = Substitute.For<IPriceUpdateAutomationService>();
        var mockItemCancelAndSellService = Substitute.For<IItemCancelAndSellService>();
        var mockLocalization = Substitute.For<ILocalizationService>();

        using var service = new RetainerContextMenuService(mockContextMenu, mockPriceService, mockItemCancelAndSellService, mockLocalization);

        var mockArgs = Substitute.For<IMenuOpenedArgs>();
        mockArgs.AddonName.Returns("Inventory");

        mockContextMenu.OnMenuOpened += Raise.Event<IContextMenu.OnMenuOpenedDelegate>(mockArgs);

        mockArgs.DidNotReceiveWithAnyArgs().AddMenuItem(default!);
    }
}