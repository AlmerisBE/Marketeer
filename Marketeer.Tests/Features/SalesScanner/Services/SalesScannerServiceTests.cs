using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Plugin.Services;
using Marketeer.Features.Logging.Contracts;
using Marketeer.Features.SalesHistoryTracking.Contracts;
using Marketeer.Features.SalesScanner.Services;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Features.SalesScanner.Services;

public class SalesScannerServiceTests {
    [Fact]
    public void SalesScannerService_Enable_RegistersAddonListener() {
        // Arrange
        var mockAddonLifecycle = Substitute.For<IAddonLifecycle>();
        var mockFramework = Substitute.For<IFramework>();
        var mockScraper = Substitute.For<ISalesHistoryScraper>();
        var mockRepository = Substitute.For<ISalesRepository>();
        var mockLogger = Substitute.For<ILoggerService>();

        var service = new SalesScannerService(mockAddonLifecycle, mockFramework, mockScraper, mockRepository, mockLogger);

        // Act
        service.Enable();

        // Assert
        mockAddonLifecycle.Received(1).RegisterListener(
            AddonEvent.PostSetup,
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<Dalamud.Plugin.Services.IAddonLifecycle.AddonEventDelegate>());
    }

    [Fact]
    public void SalesScannerService_Disable_UnregistersAddonListener() {
        // Arrange
        var mockAddonLifecycle = Substitute.For<IAddonLifecycle>();
        var mockFramework = Substitute.For<IFramework>();
        var mockScraper = Substitute.For<ISalesHistoryScraper>();
        var mockRepository = Substitute.For<ISalesRepository>();
        var mockLogger = Substitute.For<ILoggerService>();

        var service = new SalesScannerService(mockAddonLifecycle, mockFramework, mockScraper, mockRepository, mockLogger);

        service.Enable();

        // Act
        service.Disable();

        // Assert
        mockAddonLifecycle.Received(1).UnregisterListener(
            AddonEvent.PostSetup,
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<Dalamud.Plugin.Services.IAddonLifecycle.AddonEventDelegate>());
    }
}