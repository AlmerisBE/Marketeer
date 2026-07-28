using Dalamud.Plugin.Services;
using Marketeer.API.GameInterop.Contracts;
using Marketeer.API.Localization.Contracts;
using Marketeer.API.RetainerAutomation.Contracts;
using Marketeer.Core.RetainerAutomation.Services;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Core.RetainerAutomation.Services;

public class ItemCancelAndSellServiceTests {

    [Fact]
    public void TriggerCancelAndSell_WithUnknownTarget_SelectsImmediateReturnOption() {
        var mockUiInteraction = Substitute.For<IRetainerUiInteractionService>();
        var mockInventoryService = Substitute.For<IInventoryService>();
        var mockLocalization = Substitute.For<ILocalizationService>();
        var mockFramework = Substitute.For<IFramework>();

        mockLocalization.Translate("RetainerMenu_ReturnToInventory").Returns("Mettre dans l'inventaire");
        mockUiInteraction.GetContextMenuItemIndex("Mettre dans l'inventaire").Returns(2);

        var service = new ItemCancelAndSellService(mockUiInteraction, mockInventoryService, mockLocalization, mockFramework);

        // Act - Simulate clicking with no identifiable target (Fallback path)
        service.TriggerCancelAndSell(null);

        // Assert
        mockUiInteraction.Received(1).SelectContextMenuItem(2);
    }
}