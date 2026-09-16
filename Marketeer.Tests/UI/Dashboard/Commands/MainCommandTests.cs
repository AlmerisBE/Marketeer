using Marketeer.Core.Financials.Contracts;
using Marketeer.UI.Financials.UI;
using Marketeer.UI.Localization.Contracts;
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