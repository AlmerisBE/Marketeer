using Dalamud.Plugin.Services;
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
    public void TriggerScan_WhenRetainersExist_ExecutesFullLoopSuccessfully() {
        // Arrange
        var mockFramework = Substitute.For<IFramework>();
        var mockRetainerService = Substitute.For<IRetainerService>();
        var mockRetainerProvider = Substitute.For<IRetainerProvider>();
        var mockWindowService = Substitute.For<INativeWindowService>();
        var mockLocalization = Substitute.For<ILocalizationService>();
        var mockLogger = Substitute.For<ILoggerService>();

        var mockRetainerList = Substitute.For<INativeWindow>();
        mockRetainerList.IsVisible.Returns(true);
        mockWindowService.GetWindow("RetainerList").Returns(mockRetainerList);

        var mockRetainerSellList = Substitute.For<INativeWindow>();
        mockRetainerSellList.IsVisible.Returns(true);
        mockWindowService.GetWindow("RetainerSellList").Returns(mockRetainerSellList);

        var mockSelectString = Substitute.For<INativeWindow>();
        mockSelectString.IsVisible.Returns(true);
        mockWindowService.GetWindow("SelectString").Returns(mockSelectString);

        var retainers = new List<TrackedRetainer> {
            new TrackedRetainer { Name = "AlmerisRetainer" }
        };
        mockRetainerProvider.GetActiveRetainers().Returns(retainers);

        mockLocalization.Translate("RetainerMenu_SellItems").Returns("Sell items");

        mockRetainerService.IsRetainerAvailable("AlmerisRetainer").Returns(true);
        mockRetainerService.IsMenuReadyForRetainer("AlmerisRetainer").Returns(true);
        mockRetainerService.IsMenuOptionAvailable("Sell items").Returns(true);

        mockRetainerService.SelectRetainer("AlmerisRetainer").Returns(true);
        mockRetainerService.SelectMenuOption("Sell items").Returns(true);

        mockRetainerService.CloseMarketListings().Returns(true);
        mockRetainerService.CloseRetainerMenu().Returns(true);

        var service = new RetainerAutomationService(
            mockFramework, mockRetainerService, mockRetainerProvider,
            mockWindowService, mockLocalization, mockLogger);

        // Act
        service.TriggerScan();

        for (int i = 0; i <= 250; i++) {
            mockFramework.Update += Raise.Event<IFramework.OnUpdateDelegate>(mockFramework);
        }

        // Assert
        mockRetainerService.Received(1).SelectRetainer("AlmerisRetainer");
        mockRetainerService.Received(1).SelectMenuOption("Sell items");
        mockRetainerService.Received(1).CloseMarketListings();

        // Assert that the native cancel method was called to prevent ghost windows
        mockRetainerService.Received(1).CloseRetainerMenu();

        mockRetainerList.Received(1).SendCallback(-1);
        Assert.False(service.IsScanning);
    }
}