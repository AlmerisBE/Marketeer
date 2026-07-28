using Dalamud.Plugin.Services;
using Marketeer.API.CharacterManagement.Models;
using Marketeer.API.GameInterop.Contracts;
using Marketeer.API.InventoryTracking.Contracts;
using Marketeer.API.InventoryTracking.Models;
using Marketeer.API.Logging.Contracts;
using Marketeer.API.UiInterop.Contracts;
using Marketeer.Core.InventoryTracking.Services;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Core.InventoryTracking.Services;

public class RetainerInventoryTrackerServiceTests {
    [Fact]
    public void OnFrameworkUpdate_WhenSelectStringNotVisible_ShouldNotCaptureSnapshot() {
        var mockFramework = Substitute.For<IFramework>();
        var mockListingProvider = Substitute.For<IMarketListingProvider>();
        var mockRetainerProvider = Substitute.For<IRetainerProvider>();
        var mockSnapshotService = Substitute.For<IInventorySnapshotService>();
        var mockWindowService = Substitute.For<INativeWindowService>();
        var mockLogger = Substitute.For<ILoggerService>();

        mockListingProvider.GetActiveRetainerId().Returns(12345ul);

        var mockWindow = Substitute.For<INativeWindow>();
        mockWindow.IsVisible.Returns(false);
        mockWindowService.GetWindow("SelectString").Returns(mockWindow);

        using var service = new RetainerInventoryTrackerService(
            mockFramework, mockListingProvider, mockRetainerProvider,
            mockSnapshotService, mockWindowService, mockLogger);

        mockSnapshotService.DidNotReceiveWithAnyArgs().SaveRetainerSnapshot(default, default!);
    }

    [Fact]
    public void OnFrameworkUpdate_WhenSelectStringVisible_ShouldCaptureAndSaveSnapshot() {
        var mockFramework = Substitute.For<IFramework>();
        var mockListingProvider = Substitute.For<IMarketListingProvider>();
        var mockRetainerProvider = Substitute.For<IRetainerProvider>();
        var mockSnapshotService = Substitute.For<IInventorySnapshotService>();
        var mockWindowService = Substitute.For<INativeWindowService>();
        var mockLogger = Substitute.For<ILoggerService>();

        mockListingProvider.GetActiveRetainerId().Returns(12345ul);

        var retainers = new List<TrackedRetainer> {
            new TrackedRetainer { RetainerId = 12345ul, Name = "Adelaide" }
        };
        mockRetainerProvider.GetActiveRetainers().Returns(retainers);

        var mockWindow = Substitute.For<INativeWindow>();
        mockWindow.IsVisible.Returns(true);
        mockWindowService.GetWindow("SelectString").Returns(mockWindow);

        var snapshot = new InventorySnapshot { Items = new List<TrackedItem> { new TrackedItem() } };
        mockSnapshotService.CreateRetainerSnapshot(12345ul, "Adelaide").Returns(snapshot);

        var service = new RetainerInventoryTrackerService(
            mockFramework, mockListingProvider, mockRetainerProvider,
            mockSnapshotService, mockWindowService, mockLogger);

        // Simulate Framework Update call
        mockFramework.Update += Raise.Event<IFrameworkUpdateDelegate>(mockFramework);

        mockSnapshotService.Received(1).SaveRetainerSnapshot(12345ul, snapshot);
    }
}