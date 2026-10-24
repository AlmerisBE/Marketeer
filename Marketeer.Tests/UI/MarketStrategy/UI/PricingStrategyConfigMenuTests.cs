using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;
using Marketeer.Core.Configuration.Contracts;
using Marketeer.Core.Configuration.Models;
using Marketeer.UI.Localization.Contracts;
using Marketeer.UI.MarketStrategy.UI;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.UI.MarketStrategy.UI;

public class PricingStrategyConfigMenuTests {
    private IConfigurationService configService;
    private ILocalizationService localizationService;
    private IDataManager dataManager;

    public PricingStrategyConfigMenuTests() {
        this.configService = Substitute.For<IConfigurationService>();
        this.localizationService = Substitute.For<ILocalizationService>();
        this.dataManager = Substitute.For<IDataManager>();

        this.configService.GetConfig().Returns(new PluginConfiguration());

        // Setup localization mock to return the requested key
        this.localizationService.Translate(Arg.Any<string>()).Returns(callInfo => callInfo.Arg<string>());

        // Mock the Excel sheet to prevent null reference exceptions during component initialization
        this.dataManager.GetExcelSheet<ItemSearchCategory>().Returns((IEnumerable<ItemSearchCategory>?)null);
    }

    private PricingStrategyConfigMenu CreateMenu() {
        return new PricingStrategyConfigMenu(this.configService, this.localizationService, this.dataManager);
    }

    [Fact]
    public void Constructor_InitializesCorrectly_AndLoadsCategories() {
        var menu = this.CreateMenu();

        Assert.Equal("Group_Configuration", menu.GroupName);
        Assert.Equal("Config_Competition_TabName", menu.Name);
        Assert.Equal(100, menu.Priority);
        Assert.True(menu.HasContent);
        Assert.False(menu.DefaultExpanded);
        Assert.Empty(menu.GetChildren());
    }
}