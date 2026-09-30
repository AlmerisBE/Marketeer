using Marketeer.Core.CompetitionTracking.Contracts;
using Marketeer.UI.CompetitionTracking.UI;
using Marketeer.UI.Localization.Contracts;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.UI.CompetitionTracking.UI;

public class CompetitionTabTests {
    [Fact]
    public void Name_ReturnsTranslatedTabName() {
        var competitionState = Substitute.For<ICompetitionStateService>();
        var localization = Substitute.For<ILocalizationService>();

        // Mock the new consolidated translation key mapped during the UI refactor
        localization.Translate("Menu_Competition").Returns("Concurrence");

        var menu = new CompetitionMenu(competitionState, localization);

        Assert.Equal("Concurrence", menu.Name);
    }
}