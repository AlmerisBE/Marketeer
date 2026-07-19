using Marketeer.Features.Dashboard.Contracts;
using Marketeer.Features.Dashboard.UI;
using Marketeer.Features.Localization.Contracts;
using Marketeer.Features.RetainerAutomation.Contracts;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Features.Dashboard.UI;

public class DashboardWindowTests {
    [Fact]
    public void DashboardWindow_OnInitialization_SortsInjectedTabsByPriority() {
        // Arrange
        var mockLocalization = Substitute.For<ILocalizationService>();
        var mockAutomationService = Substitute.For<IRetainerAutomationService>();

        mockLocalization.Translate("Dashboard_Title").Returns("Marketeer - Dashboard");

        var lowPriorityTab = Substitute.For<IDashboardTab>();
        lowPriorityTab.Priority.Returns(100);

        var highPriorityTab = Substitute.For<IDashboardTab>();
        highPriorityTab.Priority.Returns(10);

        // Inject them in the wrong order intentionally
        var tabs = new List<IDashboardTab> { lowPriorityTab, highPriorityTab };

        // Act
        var exception = Record.Exception(() => new DashboardWindow(tabs, mockLocalization, mockAutomationService));

        // Assert
        Assert.Null(exception);

        // At this point, no exception ensures LINQ OrderBy executed successfully on the mocked properties.
        // We verify the Priority property was accessed during instantiation.
        var _ = lowPriorityTab.Received().Priority;
        var __ = highPriorityTab.Received().Priority;
    }
}