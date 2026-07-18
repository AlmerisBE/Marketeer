using Marketeer.Features.Dashboard.Contracts;
using Marketeer.Features.Dashboard.UI;
using Marketeer.Features.Financials.Contracts;
using Marketeer.Features.Financials.UI;
using Marketeer.Features.Localization.Contracts;
using Marketeer.Features.RetainerAutomation.Contracts;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Features.Dashboard.UI;

public class DashboardWindowTests {
    [Fact]
    public void DashboardWindow_OnInitialization_AcceptsFinancialTabAndAutomationDependencies() {
        // Arrange
        var mockLocalization = Substitute.For<ILocalizationService>();
        var mockFinancialService = Substitute.For<IFinancialService>();
        var mockAutomationService = Substitute.For<IRetainerAutomationService>();
        var widgets = new List<IDashboardWidget>();

        mockLocalization.Translate("Dashboard_Title").Returns("Marketeer - Dashboard");
        var financialTab = new FinancialsTab(mockFinancialService, mockLocalization);

        // Act
        var exception = Record.Exception(() => new DashboardWindow(widgets, mockLocalization, financialTab, mockAutomationService));

        // Assert
        Assert.Null(exception);
    }
}