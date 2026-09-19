using Dalamud.Game.ClientState.Keys;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Marketeer.Core.Configuration.Contracts;
using Marketeer.Core.Configuration.Models;
using Marketeer.UI.Shell.Contracts;
using Marketeer.UI.Shell.Services;
using Marketeer.UI.Shell.UI;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.UI.Shell.Services;

public class HotkeyTrackerServiceTests {
    [Fact]
    public void Update_WhenConfiguredHotkeyAndModifiersMatch_TogglesMainWindow() {
        var configService = Substitute.For<IConfigurationService>();
        var keyState = Substitute.For<IKeyState>();
        var framework = Substitute.For<IFramework>();
        var pluginInterface = Substitute.For<IDalamudPluginInterface>();
        var navService = Substitute.For<INavigationService>();

        var config = new PluginConfiguration {
            DashboardHotkey = VirtualKey.G,
            DashboardHotkeyCtrl = true,
            DashboardHotkeyAlt = false,
            DashboardHotkeyShift = false
        };
        configService.GetConfig().Returns(config);

        var mainWindow = new MainWindow(
            pluginInterface,
            navService,
            new List<INavigationNode>(),
            new List<IToolbarAction>(),
            new List<IStatusBarProvider>()
        );

        mainWindow.IsOpen = false;

        IFramework.OnUpdateDelegate? capturedUpdate = null;
        framework.When(f => f.Update += Arg.Any<IFramework.OnUpdateDelegate>())
                 .Do(callInfo => capturedUpdate = callInfo.Arg<IFramework.OnUpdateDelegate>());

        var service = new HotkeyTrackerService(framework, keyState, configService, mainWindow);

        keyState[VirtualKey.G].Returns(true);
        keyState[VirtualKey.CONTROL].Returns(true);
        keyState[VirtualKey.MENU].Returns(false);
        keyState[VirtualKey.SHIFT].Returns(false);

        if (capturedUpdate != null) capturedUpdate.Invoke(framework);

        Assert.True(mainWindow.IsOpen);
    }
}