using Marketeer.Core.Configuration.Contracts;
using Marketeer.UI.CompetitionTracking.UI;
using Marketeer.UI.Localization.Contracts;
using Marketeer.UI.Shell.Contracts;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.UI.CompetitionTracking.UI;

public class CompetitionConfigMenuTests {
    [Fact]
    public void Constructor_ShouldInitializeNavigationNodeProperly() {
        var configService = Substitute.For<IConfigurationService>();
        var localization = Substitute.For<ILocalizationService>();

        localization.Translate("Config_Competition_TabName").Returns("Pricing");
        localization.Translate("Group_Configuration").Returns("Configuration");

        var menu = new CompetitionConfigMenu(configService, localization);

        Assert.IsAssignableFrom<INavigationNode>(menu);
        Assert.Equal("Pricing", menu.Name);
        Assert.Equal("Configuration", menu.GroupName);
        Assert.True(menu.HasContent);
        Assert.False(menu.DefaultExpanded);
        Assert.Empty(menu.GetChildren());
    }
}