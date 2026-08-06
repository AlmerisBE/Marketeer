using Marketeer.API.Configuration.Contracts;
using Marketeer.API.Configuration.Models;
using Marketeer.API.CraftingProfit.Models;
using Marketeer.Core.CraftingProfit.Repositories;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Core.CraftingProfit.Repositories;

public class CraftingProfitRepositoryTests {
    private IConfigurationService configService;
    private PluginConfiguration config;

    public CraftingProfitRepositoryTests() {
        this.configService = Substitute.For<IConfigurationService>();
        this.config = new PluginConfiguration { CraftingItems = new Dictionary<uint, CraftingItemConfig>() };
        this.configService.GetConfig().Returns(this.config);
    }

    [Fact]
    public void SaveConfig_ShouldAddOrUpdateConfig_AndSave() {
        var repository = new CraftingProfitRepository(this.configService);
        var itemConfig = new CraftingItemConfig { ItemId = 123, TargetSellPrice = 5000 };

        repository.SaveConfig(itemConfig);

        Assert.True(this.config.CraftingItems.ContainsKey(123));
        this.configService.Received(1).Save();
    }

    [Fact]
    public void RemoveConfig_ShouldRemoveItem_AndSave() {
        var repository = new CraftingProfitRepository(this.configService);
        this.config.CraftingItems[123] = new CraftingItemConfig { ItemId = 123 };

        repository.RemoveConfig(123);

        Assert.False(this.config.CraftingItems.ContainsKey(123));
        this.configService.Received(1).Save();
    }
}