using Dalamud.Plugin;
using Marketeer.UI.Localization.Contracts;
using Marketeer.UI.Shell.Commands;
using Marketeer.UI.Shell.Contracts;
using Marketeer.UI.Shell.UI;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.UI.Shell.Commands;

public class ConfigCommandTests {
    [Fact]
    public void Execute_ShouldOpenMainWindow() {
        // Mock requirements to instantiate the real MainWindow safely
        var pluginInterface = Substitute.For<IDalamudPluginInterface>();
        var navService = Substitute.For<INavigationService>();
        var nodes = new List<INavigationNode>();
        var toolbarActions = new List<IToolbarAction>();
        var statusBarProviders = new List<IStatusBarProvider>();

        var mainWindow = new MainWindow(pluginInterface, navService, nodes, toolbarActions, statusBarProviders);
        var localizationService = Substitute.For<ILocalizationService>();

        var command = new ConfigCommand(mainWindow, localizationService);

        mainWindow.IsOpen = false;

        command.Execute(string.Empty);

        Assert.True(mainWindow.IsOpen);
    }
}