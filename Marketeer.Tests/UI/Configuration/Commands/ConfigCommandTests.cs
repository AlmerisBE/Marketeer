using Dalamud.Plugin.Services;
using Marketeer.Core.Configuration.Contracts;
using Marketeer.UI.Configuration.Commands;
using Marketeer.UI.Configuration.UI;
using Marketeer.UI.Localization.Contracts;
using Marketeer.UI.Shell.Contracts;
using Marketeer.UI.Shell.UI;
using Marketeer.UI.Themes.Contracts;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.UI.Configuration.Commands;

public class ConfigCommandTests {
    [Fact]
    public void Execute_ShouldNavigateToConfigMenuAndOpenWindow() {
        var navService = Substitute.For<INavigationService>();
        var localization = Substitute.For<ILocalizationService>();
        var configService = Substitute.For<IConfigurationService>();
        var mockKeyState = Substitute.For<IKeyState>();
        var mockThemeService = Substitute.For<IThemeService>();

        var configMenu = new ConfigMenu(configService, localization, mockKeyState, mockThemeService);

        var mainWindow = new MainWindow(new List<INavigationNode>(), new List<ISidebarAction>(), localization, navService);

        var command = new ConfigCommand(navService, mainWindow, configMenu, localization);

        command.Execute("");

        navService.Received(1).NavigateTo(configMenu);
        Assert.True(mainWindow.IsOpen);
    }
}