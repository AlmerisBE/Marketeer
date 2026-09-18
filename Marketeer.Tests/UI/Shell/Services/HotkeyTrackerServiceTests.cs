using Dalamud.Game.ClientState.Keys;
using Dalamud.Plugin.Services;
using Marketeer.Core.Configuration.Contracts;
using Marketeer.Core.Configuration.Models;
using Marketeer.UI.Shell.Services;
using Marketeer.UI.Shell.UI;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.UI.Shell.Services;

public class HotkeyTrackerServiceTests {
    [Fact]
    public void Update_WhenConfiguredHotkeyAndModifiersMatch_TogglesMainWindow() {
        var framework = Substitute.For<IFramework>();
        var keyState = Substitute.For<IKeyState>();
        var configService = Substitute.For<IConfigurationService>();

        var config = new PluginConfiguration {
            DashboardHotkey = VirtualKey.M,
            DashboardHotkeyCtrl = true,
            DashboardHotkeyAlt = false,
            DashboardHotkeyShift = true
        };
        configService.GetConfig().Returns(config);

        keyState[VirtualKey.M].Returns(true);
        keyState[VirtualKey.CONTROL].Returns(true);
        keyState[VirtualKey.MENU].Returns(false);
        keyState[VirtualKey.SHIFT].Returns(true);

        var mainWindow = Substitute.ForPartsOf<MainWindow>(
            new List<Marketeer.UI.Shell.Contracts.INavigationNode>(),
            new List<Marketeer.UI.Shell.Contracts.ISidebarAction>(),
            Substitute.For<Marketeer.UI.Localization.Contracts.ILocalizationService>(),
            Substitute.For<Marketeer.UI.Shell.Contracts.INavigationService>());

        var service = new HotkeyTrackerService(framework, keyState, configService, mainWindow);

        // Trigger the event manually
        framework.Update += Raise.Event<IFramework.OnUpdateDelegate>(framework);

        // Assert that the window toggle mechanism was invoked
        mainWindow.Received(1).Toggle();
    }
}