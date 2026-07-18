using Marketeer.Features.Dashboard.Contracts;
using Marketeer.Features.Dashboard.UI;
using Marketeer.Features.Financials.Contracts;
using Marketeer.Features.Financials.UI;
using Marketeer.Features.Localization.Contracts;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Features.Dashboard.UI;

public class DashboardWindowTests {
    [Fact]
    public void DashboardWindow_OnInitialization_AcceptsFinancialTabDependency() {
        // Arrange
        var mockLocalization = Substitute.For<ILocalizationService>();
        var mockFinancialService = Substitute.For<IFinancialService>();
        var widgets = new List<IDashboardWidget>();

        mockLocalization.Translate("Dashboard_Title").Returns("Marketeer - Dashboard");
        var financialTab = new FinancialsTab(mockFinancialService, mockLocalization);

        // Act
        var exception = Record.Exception(() => new DashboardWindow(widgets, mockLocalization, financialTab));

        // Assert
        Assert.Null(exception);
    }
}