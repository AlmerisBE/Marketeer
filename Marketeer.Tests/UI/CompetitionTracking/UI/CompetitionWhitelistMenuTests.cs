using Marketeer.Core.CompetitionTracking.Contracts;
using Marketeer.Core.Configuration.Contracts;
using Marketeer.Core.Configuration.Models;
using Marketeer.UI.CompetitionTracking.UI;
using Marketeer.UI.Localization.Contracts;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.UI.CompetitionTracking.UI;

public class CompetitionWhitelistMenuTests {
    private IWhitelistManagerService whitelistManager;
    private IConfigurationService configService;
    private ILocalizationService localizationService;

    public CompetitionWhitelistMenuTests() {
        this.whitelistManager = Substitute.For<IWhitelistManagerService>();
        this.configService = Substitute.For<IConfigurationService>();
        this.localizationService = Substitute.For<ILocalizationService>();

        this.configService.GetConfig().Returns(new PluginConfiguration());

        // Setup localization mock to just return the key as the translated value for easy testing
        this.localizationService.Translate(Arg.Any<string>()).Returns(callInfo => callInfo.Arg<string>());
    }

    private CompetitionWhitelistMenu CreateMenu() {
        return new CompetitionWhitelistMenu(this.whitelistManager, this.configService, this.localizationService);
    }

    [Fact]
    public void Constructor_InitializesPropertiesCorrectly() {
        var menu = this.CreateMenu();

        Assert.Equal("Group_Configuration", menu.GroupName);
        Assert.Equal("Menu_CompetitionWhitelist", menu.Name);
        Assert.Equal(102, menu.Priority);
        Assert.True(menu.HasContent);
        Assert.False(menu.DefaultExpanded);
        Assert.Empty(menu.GetChildren());
    }
}