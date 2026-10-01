using Marketeer.Core.Configuration.Contracts;
using Marketeer.UI.Localization.Contracts;
using Marketeer.UI.MarketStrategy.UI;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.UI.MarketStrategy.UI;

public class MarketStrategyConfigMenuTests {
    [Fact]
    public void Constructor_WithValidDependencies_SetsCorrectGroupName() {
        var configService = Substitute.For<IConfigurationService>();
        var localization = Substitute.For<ILocalizationService>();

        localization.Translate("Group_Configuration").Returns("Configuration");

        var menu = new MarketStrategyConfigMenu(configService, localization);

        Assert.NotNull(menu);
        Assert.Equal("Configuration", menu.GroupName);
        Assert.True(menu.HasContent);
    }
}