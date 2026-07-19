using Marketeer.Features.Dashboard.Contracts;
using Marketeer.Features.Dashboard.UI;
using Marketeer.Features.Localization.Contracts;
using Marketeer.Features.RetainerAutomation.Contracts;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Features.Dashboard.UI;

public class DashboardWindowTests {
    [Fact]
    public void DashboardWindow_OnInitialization_AcceptsInjectedTabs() {
        // Arrange
        var mockLocalization = Substitute.For<ILocalizationService>();
        var mockAutomationService = Substitute.For<IRetainerAutomationService>();
        var tabs = new List<IDashboardTab>(); // Simulated DI enumerable aggregation

        mockLocalization.Translate("Dashboard_Title").Returns("Marketeer - Dashboard");

        // Act
        var exception = Record.Exception(() => new DashboardWindow(tabs, mockLocalization, mockAutomationService));

        // Assert
        Assert.Null(exception);
    }
}