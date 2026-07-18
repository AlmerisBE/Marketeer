using Marketeer.Features.Financials.Contracts;
using Marketeer.Features.Financials.UI;
using Marketeer.Features.Localization.Contracts;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Features.Dashboard.Commands;

public class MainCommandTests {
    [Fact]
    public void MainCommand_ShouldPassMockDependenciesToFinancialsTab() {
        // Arrange
        var mockFinancialService = Substitute.For<IFinancialService>();
        var mockLocalizationService = Substitute.For<ILocalizationService>();

        // Act
        var exception = Record.Exception(() => new FinancialsTab(mockFinancialService, mockLocalizationService));

        // Assert
        Assert.Null(exception);
    }
}