using Dalamud.Plugin.Services;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using Marketeer.Core.Configuration.Contracts;
using Marketeer.Core.Configuration.Models;
using Marketeer.UI.Localization.Contracts;
using Marketeer.UI.MarketStrategy.UI;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.UI.MarketStrategy.UI;

public class MarketStrategyConfigMenuTests {
    private IConfigurationService configService;
    private ILocalizationService localizationService;
    private IDataManager dataManager;

    public MarketStrategyConfigMenuTests() {
        this.configService = Substitute.For<IConfigurationService>();
        this.localizationService = Substitute.For<ILocalizationService>();
        this.dataManager = Substitute.For<IDataManager>();

        this.configService.GetConfig().Returns(new PluginConfiguration());
        this.localizationService.Translate(Arg.Any<string>()).Returns(callInfo => callInfo.Arg<string>());

        // Since Lumina's ExcelSheet<T> is sealed, NSubstitute cannot mock it natively.
        // Fortunately, the MarketStrategyConfigMenu implementation handles null sheets gracefully.
        this.dataManager.GetExcelSheet<ItemSearchCategory>().Returns((ExcelSheet<ItemSearchCategory>?)null);
    }

    [Fact]
    public void Constructor_InitializesCorrectly_AndLoadsCategories() {
        var menu = new PricingStrategyConfigMenu(this.configService, this.localizationService, this.dataManager);

        Assert.NotNull(menu);
        Assert.True(menu.HasContent);
        Assert.Equal("Menu_MarketStrategy", menu.Name);
        Assert.Equal("Group_Configuration", menu.GroupName);
        Assert.Equal(101, menu.Priority);

        // Verify that the constructor properly requested the category sheet from Lumina
        this.dataManager.Received(1).GetExcelSheet<ItemSearchCategory>();
    }
}