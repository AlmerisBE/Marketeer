using Marketeer.UI.Localization.Contracts;
using Marketeer.UI.Shell.Commands;
using Marketeer.UI.Shell.Contracts;
using Marketeer.UI.Shell.UI;
using Marketeer.UI.Themes.Contracts;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.UI.Shell.Commands;

public class MainCommandTests {
    [Fact]
    public void Execute_ShouldToggleWindow() {
        var navService = Substitute.For<INavigationService>();
        var localization = Substitute.For<ILocalizationService>();
        var mockThemeService = Substitute.For<IThemeService>();
        var mainWindow = new MainWindow(new List<INavigationNode>(), new List<ISidebarAction>(), localization, navService, mockThemeService);

        var command = new MainCommand(mainWindow);

        bool initialStatus = mainWindow.IsOpen;
        command.Execute("");

        Assert.NotEqual(initialStatus, mainWindow.IsOpen);
    }
}