using Marketeer.UI.Localization.Contracts;
using Marketeer.UI.Shell.Contracts;
using Marketeer.UI.Shell.UI;
using Marketeer.UI.Themes.Contracts;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.UI.Shell.UI;

public class MainWindowTests {
    [Fact]
    public void Constructor_WhenNoSelectedNode_NavigatesToFirstNode() {
        var localization = Substitute.For<ILocalizationService>();
        var navService = Substitute.For<INavigationService>();
        var themeService = Substitute.For<IThemeService>();

        var node = Substitute.For<INavigationNode>();
        node.Priority.Returns(1);

        navService.SelectedNode.Returns((INavigationNode?)null);

        var window = new MainWindow(new[] { node }, new List<ISidebarAction>(), localization, navService, themeService);

        navService.Received(1).NavigateTo(node);
    }
}