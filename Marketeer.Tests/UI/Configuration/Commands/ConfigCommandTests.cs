using Marketeer.Core.Configuration.Contracts;
using Marketeer.Core.RetainerAutomation.Contracts;
using Marketeer.UI.Configuration.Commands;
using Marketeer.UI.Configuration.UI;
using Marketeer.UI.Dashboard.UI;
using Marketeer.UI.Localization.Contracts;
using Marketeer.UI.Shell.Contracts;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.UI.Configuration.Commands;

public class ConfigCommandTests {
    [Fact]
    public void Execute_NavigatesToConfigMenuAndOpensDashboard() {
        // Arrange
        var mockNavService = Substitute.For<INavigationService>();
        var mockConfigService = Substitute.For<IConfigurationService>();
        var mockLocalization = Substitute.For<ILocalizationService>();

        var configMenu = new ConfigMenu(mockConfigService, mockLocalization);

        // Dummy dashboard window dependencies
        var mockNodes = new List<INavigationNode>();
        var mockAutomation = Substitute.For<IRetainerAutomationService>();
        var dashboardWindow = new DashboardWindow(mockNodes, mockLocalization, mockAutomation, mockNavService);

        var command = new ConfigCommand(mockNavService, dashboardWindow, configMenu, mockLocalization);

        // Act
        command.Execute(string.Empty);

        // Assert
        mockNavService.Received(1).NavigateTo(configMenu);
        Assert.True(dashboardWindow.IsOpen);
    }
}