using Marketeer.API.Localization.Contracts;
using Marketeer.API.RetainerAutomation.Contracts;
using Marketeer.API.CompetitionTracking.Contracts;
using Marketeer.UI.CompetitionTracking.UI;
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