using Marketeer.Features.Localization.Contracts;
using Marketeer.Features.RetainerAutomation.Contracts;
using Marketeer.Features.UndercutTracking.Contracts;
using Marketeer.Features.UndercutTracking.UI;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Features.UndercutTracking.UI;

public class CompetitionTabTests {
    [Fact]
    public void Name_ReturnsTranslatedTabName() {
        // Arrange
        var mockCompetitionState = Substitute.For<ICompetitionStateService>();
        var mockLocalization = Substitute.For<ILocalizationService>();
        var mockAutomation = Substitute.For<IPriceUpdateAutomationService>();

        mockLocalization.Translate("Undercuts_TabName").Returns("Concurrence");

        var tab = new CompetitionTab(mockCompetitionState, mockLocalization, mockAutomation);

        // Act
        var result = tab.Name;

        // Assert
        Assert.Equal("Concurrence", result);
        Assert.Equal(40, tab.Priority);
    }
}