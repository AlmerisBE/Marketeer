using Dalamud.Plugin.Services;
using Marketeer.Features.Dashboard.Contracts;
using Marketeer.Features.Localization.Contracts;
using Marketeer.Features.Logging.Contracts;
using Marketeer.Features.RetainerAutomation.Services;
using Marketeer.Features.Retainers.Contracts;
using Marketeer.Features.RetainerTracking.Contracts;
using Marketeer.Features.RetainerTracking.Models;
using Marketeer.Features.WindowAbstraction.Contracts;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Features.RetainerAutomation.Services;

public class RetainerAutomationServiceTests {

    [Fact]
    public void TriggerScan_ExecutesMarketScanWithoutOpeningSalesHistory_AndPassesFirstScanFlag() {
        // Arrange
        var mockFramework = Substitute.For<IFramework>();
        var mockRetainerService = Substitute.For<IRetainerService>();
        var mockRetainerProvider = Substitute.For<IRetainerProvider>();
        var mockWindowService = Substitute.For<INativeWindowService>();
        var mockLocalization = Substitute.For<ILocalizationService>();
        var mockMarketTracker = Substitute.For<IMarketListingTrackerService>();
        var mockLogger = Substitute.For<ILoggerService>();

        var mockRetainerList = Substitute.For<INativeWindow>();
        mockRetainerList.IsVisible.Returns(true);
        mockWindowService.GetWindow("RetainerList").Returns(mockRetainerList);

        var retainers = new List<TrackedRetainer> {
            new TrackedRetainer { Name = "AlmerisRetainer", RetainerId = 1001 }
        };
        mockRetainerProvider.GetActiveRetainers().Returns(retainers);

        mockLocalization.Translate("RetainerMenu_SellItems").Returns("Sell items");

        mockRetainerService.IsRetainerAvailable("AlmerisRetainer").Returns(true);
        mockRetainerService.SelectRetainer("AlmerisRetainer").Returns(true);

        mockRetainerService.IsMenuReadyForRetainer("AlmerisRetainer").Returns(true);
        mockRetainerService.IsMenuOptionAvailable("Sell items").Returns(true);
        mockRetainerService.SelectMenuOption("Sell items").Returns(true);

        mockMarketTracker.ScanListings(1001, Arg.Any<bool>()).Returns(true);

        mockRetainerService.CloseMarketListings().Returns(true);
        mockRetainerService.CloseRetainerMenu().Returns(true);

        var service = new RetainerAutomationService(
            mockFramework, mockRetainerService, mockRetainerProvider,
            mockWindowService, mockLocalization, mockMarketTracker, mockLogger);

        // Act
        service.TriggerScan();

        // Advance framework loops enough times to exhaust all cooldowns across the state machine
        for (int i = 0; i <= 500; i++) {
            mockFramework.Update += Raise.Event<IFramework.OnUpdateDelegate>(mockFramework);
        }

        // Assert
        mockRetainerService.Received(1).SelectRetainer("AlmerisRetainer");
        mockRetainerService.Received(1).SelectMenuOption("Sell items");

        // Verify the scanner was called exactly once with isFirstScan = true
        mockMarketTracker.Received(1).ScanListings(1001, true);

        // Verify Sales History was explicitly NOT called
        mockRetainerService.DidNotReceive().CloseSalesHistory();

        mockRetainerService.Received(1).CloseMarketListings();
        mockRetainerService.Received(1).CloseRetainerMenu();
        mockRetainerList.Received(1).SendCallback(-1);

        Assert.False(service.IsScanning);
    }
}