using Dalamud.Plugin;
using Marketeer.UI.Localization.Contracts;
using Marketeer.UI.Shell.Commands;
using Marketeer.UI.Shell.Contracts;
using Marketeer.UI.Shell.UI;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.UI.Shell.Commands;

public class MainCommandTests {
    [Fact]
    public void Execute_TogglesMainWindowVisibility() {
        var navService = Substitute.For<INavigationService>();
        var pluginInterface = Substitute.For<IDalamudPluginInterface>();

        var mainWindow = new MainWindow(
            pluginInterface,
            navService,
            new List<INavigationNode>(),
            new List<IToolbarAction>(),
            new List<IStatusBarProvider>()
        );

        var localization = Substitute.For<ILocalizationService>();
        var command = new MainCommand(mainWindow);

        mainWindow.IsOpen = false;
        command.Execute(string.Empty);
        Assert.True(mainWindow.IsOpen);

        command.Execute(string.Empty);
        Assert.False(mainWindow.IsOpen);
    }
}