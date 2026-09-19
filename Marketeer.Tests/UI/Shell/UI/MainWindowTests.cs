using Dalamud.Plugin;
using Marketeer.UI.Shell.Contracts;
using Marketeer.UI.Shell.UI;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.UI.Shell.UI;

public class MainWindowTests {
    [Fact]
    public void Constructor_InitializesSuccessfully() {
        var pluginInterface = Substitute.For<IDalamudPluginInterface>();
        var navService = Substitute.For<INavigationService>();
        var mockNode = Substitute.For<INavigationNode>();

        navService.CurrentNode.Returns(mockNode);

        var mainWindow = new MainWindow(
            pluginInterface,
            navService,
            new List<INavigationNode>(),
            new List<IToolbarAction>(),
            new List<IStatusBarProvider>()
        );

        Assert.NotNull(mainWindow);
        Assert.Equal(mockNode, navService.CurrentNode);
    }
}