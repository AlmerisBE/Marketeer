using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Plugin.Services;
using Marketeer.Core.Logging.Contracts;
using Marketeer.UI.UiInterop.Services;
using NSubstitute;
using Xunit;
using static Dalamud.Plugin.Services.IAddonLifecycle;

namespace Marketeer.Tests.Core.UiInterop.Services;

public class WindowTrackerServiceTests {
    [Fact]
    public void WindowTrackerService_EnableTracking_RegistersListeners() {
        // Arrange
        var mockLifecycle = Substitute.For<IAddonLifecycle>();
        var mockFramework = Substitute.For<IFramework>();
        var mockLogger = Substitute.For<ILoggerService>();

        var service = new WindowTrackerService(mockLifecycle, mockFramework, mockLogger);

        // Act
        service.EnableTracking();

        // Assert
        Assert.True(service.IsTracking);
        mockLifecycle.Received(1).RegisterListener(AddonEvent.PostSetup, Arg.Any<AddonEventDelegate>());
        mockLifecycle.Received(1).RegisterListener(AddonEvent.PreFinalize, Arg.Any<AddonEventDelegate>());
    }

    [Fact]
    public void WindowTrackerService_DisableTracking_UnregistersListeners() {
        // Arrange
        var mockLifecycle = Substitute.For<IAddonLifecycle>();
        var mockFramework = Substitute.For<IFramework>();
        var mockLogger = Substitute.For<ILoggerService>();

        var service = new WindowTrackerService(mockLifecycle, mockFramework, mockLogger);
        service.EnableTracking();

        // Act
        service.DisableTracking();

        // Assert
        Assert.False(service.IsTracking);
        mockLifecycle.Received(1).UnregisterListener(AddonEvent.PostSetup, Arg.Any<AddonEventDelegate>());
        mockLifecycle.Received(1).UnregisterListener(AddonEvent.PreFinalize, Arg.Any<AddonEventDelegate>());
    }
}