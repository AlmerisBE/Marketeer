using Marketeer.Core.CompetitionTracking.Contracts;
using Marketeer.Core.RetainerAutomation.Contracts;
using Marketeer.UI.CompetitionTracking.UI;
using Marketeer.UI.Localization.Contracts;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.UI.CompetitionTracking.UI;

public class CompetitionTabTests {
    [Fact]
    public void Name_ReturnsTranslatedTabName() {
        // Arrange
        var mockCompetitionState = Substitute.For<ICompetitionStateService>();
        var mockLocalization = Substitute.For<ILocalizationService>();
        var mockAutomation = Substitute.For<IPriceUpdateAutomationService>();

        mockLocalization.Translate("Undercuts_TabName").Returns("Concurrence");

        var tab = new CompetitionMenu(mockCompetitionState, mockLocalization, mockAutomation);

        // Act
        var result = tab.Name;

        // Assert
        Assert.Equal("Concurrence", result);
        Assert.Equal(40, tab.Priority);
    }
}