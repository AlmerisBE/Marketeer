using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Plugin.Services;
using Marketeer.API.Logging.Contracts;
using Marketeer.API.SalesHistory.Contracts;
using Marketeer.Core.SalesHistory.Services;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Features.SalesScanner.Services;

public class SalesScannerServiceTests {
    [Fact]
    public void SalesScannerService_Enable_RegistersAddonListeners() {
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
        mockAddonLifecycle.Received(1).RegisterListener(AddonEvent.PostSetup, Arg.Any<IEnumerable<string>>(), Arg.Any<IAddonLifecycle.AddonEventDelegate>());
        mockAddonLifecycle.Received(1).RegisterListener(AddonEvent.PreFinalize, Arg.Any<IEnumerable<string>>(), Arg.Any<IAddonLifecycle.AddonEventDelegate>());
    }

    [Fact]
    public void SalesScannerService_Disable_UnregistersAddonListeners() {
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
        mockAddonLifecycle.Received(1).UnregisterListener(AddonEvent.PostSetup, Arg.Any<IEnumerable<string>>(), Arg.Any<IAddonLifecycle.AddonEventDelegate>());
        mockAddonLifecycle.Received(1).UnregisterListener(AddonEvent.PreFinalize, Arg.Any<IEnumerable<string>>(), Arg.Any<IAddonLifecycle.AddonEventDelegate>());
    }
}