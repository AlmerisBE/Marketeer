using Marketeer.Core.Configuration.Contracts;
using Marketeer.Core.Configuration.Models;
using Marketeer.UI.CompetitionTracking.UI;
using Marketeer.UI.Localization.Contracts;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.UI.CompetitionTracking.UI;

public class CompetitionConfigMenuTests {
    [Fact]
    public void Constructor_WithValidDependencies_InstantiatesSuccessfully() {
        var configService = Substitute.For<IConfigurationService>();
        configService.GetConfig().Returns(new PluginConfiguration());
        var localization = Substitute.For<ILocalizationService>();

        // Reverted to 2 parameters
        var menu = new CompetitionConfigMenu(configService, localization);

        Assert.NotNull(menu);
    }
}