using Marketeer.Core.CompetitionTracking.Contracts;
using Marketeer.UI.CompetitionTracking.UI;
using Marketeer.UI.Localization.Contracts;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.UI.CompetitionTracking.UI;

public class CompetitionWhitelistMenuTests {
    [Fact]
    public void Constructor_WithValidDependencies_InstantiatesSuccessfully() {
        var whitelistManager = Substitute.For<IWhitelistManagerService>();
        var localization = Substitute.For<ILocalizationService>();

        var menu = new CompetitionWhitelistMenu(whitelistManager, localization);

        Assert.NotNull(menu);
        Assert.True(menu.HasContent);
    }
}