using Dalamud.Plugin.Services;
using Marketeer.API.GameInterop.Contracts;
using Marketeer.API.Localization.Contracts;
using Marketeer.API.Logging.Contracts;
using Marketeer.API.RetainerAutomation.Contracts;
using Marketeer.Core.RetainerAutomation.Services;
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
        var mockLog = Substitute.For<ILoggerService>();

        using var service = new ItemCancelAndSellService(mockUi, mockInv, mockLoc, mockFw, mockLog);

        Assert.NotNull(service);
    }
}