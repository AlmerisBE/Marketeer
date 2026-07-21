using Marketeer.API.Financials.Contracts;
using Marketeer.API.Localization.Contracts;
using Marketeer.UI.Financials.UI;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.UI.Dashboard.Commands;

public class MainCommandTests {
    [Fact]
    public void MainCommand_ShouldPassMockDependenciesToFinancialsTab() {
        // Arrange
        var mockFinancialService = Substitute.For<IFinancialService>();
        var mockLocalizationService = Substitute.For<ILocalizationService>();

        // Act
        var exception = Record.Exception(() => new FinancialsMenu(mockFinancialService, mockLocalizationService));

        // Assert
        Assert.Null(exception);
    }
}