using Dalamud.Plugin.Services;
using Marketeer.Features.Logging.Contracts;
using Marketeer.Features.RetainerAutomation.Contracts;
using Marketeer.Features.RetainerAutomation.Services;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Features.RetainerAutomation.Services;

public class RetainerAutomationServiceTests {
    [Fact]
    public void RetainerAutomationService_OnTick_ExecutesFullStateSequenceWhenReady() {
        // Arrange
        var mockUiInteraction = Substitute.For<IUiInteractionService>();
        var mockRetainerService = Substitute.For<IClientRetainerService>();
        var mockFramework = Substitute.For<IFramework>();
        var mockLogger = Substitute.For<ILoggerService>();

        mockRetainerService.GetActiveRetainerCount().Returns(1);

        // Mock all required windows as ready
        mockUiInteraction.IsAddonReady("RetainerList").Returns(true);
        mockUiInteraction.IsAddonReady("SelectString").Returns(true);
        mockUiInteraction.IsAddonReady("RetainerSell").Returns(true);

        var service = new RetainerAutomationService(mockUiInteraction, mockRetainerService, mockFramework, mockLogger);
        service.TriggerScan();

        // Act
        // Fast-forward through cooldown ticks to process all states
        for (int i = 0; i <= 200; i++) {
            mockFramework.Update += Raise.Event<IFramework.OnUpdateDelegate>(mockFramework);
        }

        // Assert
        mockUiInteraction.Received(1).SelectRetainer(0);
        mockUiInteraction.Received(1).OpenRetainerMarket();
        mockUiInteraction.Received(1).CloseRetainerMarket();
        mockUiInteraction.Received(1).CloseSelectString();

        // Assert that the service reset after hitting the max active retainer count
        Assert.False(service.IsScanning);
    }
}