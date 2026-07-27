using Dalamud.Plugin.Services;
using Marketeer.Core.Guidance.Services;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Core.Guidance.Services;

public class WindowGeometryProviderTests {

    [Fact]
    public void GetWindowGeometry_ReturnsFalse_WhenAddonNotFound() {
        var mockGameGui = Substitute.For<IGameGui>();

        // Return default struct to simulate a null pointer safely with Dalamud's updated API
        mockGameGui.GetAddonByName("NonExistentWindow").Returns(default(Dalamud.Game.NativeWrapper.AtkUnitBasePtr));

        var service = new WindowGeometryProvider(mockGameGui);

        bool result = service.GetWindowGeometry("NonExistentWindow", out var x, out var y, out var width, out var height, out var scale);

        Assert.False(result);
        Assert.Equal(0f, x);
        Assert.Equal(0f, y);
    }
}