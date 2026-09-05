using Dalamud.Plugin.Services;
using Marketeer.API.GameInterop.Contracts;
using Marketeer.API.InventoryTracking.Contracts;
using Marketeer.Core.Logging.Contracts;
using Marketeer.Core.RetainerAutomation.Contracts;
using Marketeer.Core.RetainerAutomation.Services;
using Marketeer.UI.Localization.Contracts;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Core.RetainerAutomation.Services;

public class ItemCancelAndSellServiceTests {
    [Fact]
    public void Constructor_ShouldInitializeSuccessfully() {
        var mockUi = Substitute.For<IRetainerUiInteractionService>();
        var mockInv = Substitute.For<IInventoryService>();
        var mockLoc = Substitute.For<ILocalizationService>();
        var mockFw = Substitute.For<IFramework>();
        var mockSnap = Substitute.For<IInventorySnapshotService>();
        var mockDiff = Substitute.For<IInventoryDiffService>();
        var mockLog = Substitute.For<ILoggerService>();

        using var service = new ItemCancelAndSellService(mockUi, mockInv, mockLoc, mockFw, mockSnap, mockDiff, mockLog);

        Assert.NotNull(service);
    }
}