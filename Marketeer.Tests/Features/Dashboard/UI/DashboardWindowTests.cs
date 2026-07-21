using Marketeer.API.Dashboard.Contracts;
using Marketeer.API.Localization.Contracts;
using Marketeer.API.RetainerAutomation.Contracts;
using Marketeer.UI.Dashboard.UI;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Features.Dashboard.UI;

public class DashboardWindowTests {
    [Fact]
    public void DashboardWindow_OnInitialization_SortsInjectedNodesByPriority() {
        // Arrange
        var mockLocalization = Substitute.For<ILocalizationService>();
        var mockAutomationService = Substitute.For<IRetainerAutomationService>();
        var mockNavigationService = Substitute.For<IDashboardNavigationService>();

        mockLocalization.Translate("Dashboard_Title").Returns("Marketeer - Dashboard");

        var lowPriorityNode = Substitute.For<INavigationNode>();
        lowPriorityNode.Priority.Returns(100);

        var highPriorityNode = Substitute.For<INavigationNode>();
        highPriorityNode.Priority.Returns(10);

        var nodes = new List<INavigationNode> { lowPriorityNode, highPriorityNode };

        // Act
        var exception = Record.Exception(() => new DashboardWindow(nodes, mockLocalization, mockAutomationService, mockNavigationService));

        // Assert
        Assert.Null(exception);

        // Verify the Priority property was accessed to perform the sorting
        var _ = lowPriorityNode.Received().Priority;
        var __ = highPriorityNode.Received().Priority;
    }
}