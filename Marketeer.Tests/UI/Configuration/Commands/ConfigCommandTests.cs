using Dalamud.Plugin;
using Marketeer.Core.Configuration.Contracts;
using Marketeer.UI.Configuration.Commands;
using Marketeer.UI.Configuration.UI;
using Marketeer.UI.Localization.Contracts;
using Marketeer.UI.Shell.Contracts;
using Marketeer.UI.Shell.UI;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.UI.Configuration.Commands;

public class ConfigCommandTests {
    [Fact]
    public void Execute_NavigatesToUniversalisConfigAndOpensWindow() {
        var navService = Substitute.For<INavigationService>();
        var pluginInterface = Substitute.For<IDalamudPluginInterface>();

        var mainWindow = new MainWindow(
            pluginInterface,
            navService,
            new List<INavigationNode>(),
            new List<IToolbarAction>(),
            new List<IStatusBarProvider>()
        );

        var configService = Substitute.For<IConfigurationService>();
        var localization = Substitute.For<ILocalizationService>();
        var defaultMenu = new UniversalisConfigMenu(configService, localization);

        var command = new ConfigCommand(navService, mainWindow, defaultMenu, localization);

        mainWindow.IsOpen = false;
        command.Execute(string.Empty);

        navService.Received(1).NavigateTo(defaultMenu);
        Assert.True(mainWindow.IsOpen);
    }
}