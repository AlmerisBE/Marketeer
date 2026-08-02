using Marketeer.API.Configuration.Contracts;
using Marketeer.API.Configuration.Models;
using Marketeer.API.MarketWatch.Models;
using Marketeer.Core.MarketWatch.Repositories;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Core.MarketWatch.Repositories;

public class MarketWatchRepositoryTests {
    private IConfigurationService CreateMockConfigService() {
        var configService = Substitute.For<IConfigurationService>();
        configService.GetConfig().Returns(new PluginConfiguration());
        return configService;
    }

    [Fact]
    public void AddOrUpdateItem_ShouldAddNewItemAndSave_WhenItemDoesNotExist() {
        var configService = this.CreateMockConfigService();
        var repository = new MarketWatchRepository(configService);
        var item = new WatchedItem { ItemId = 1234, TargetBuyPrice = 500 };

        repository.AddOrUpdateItem(item);

        var items = repository.GetAllWatchedItems();
        Assert.Single(items);
        Assert.Equal(1234u, items.First().ItemId);
        configService.Received(1).Save();
    }

    [Fact]
    public void AddOrUpdateItem_ShouldUpdateExistingItemAndSave_WhenItemAlreadyExists() {
        var configService = this.CreateMockConfigService();
        var repository = new MarketWatchRepository(configService);
        repository.AddOrUpdateItem(new WatchedItem { ItemId = 1234, TargetBuyPrice = 500 });

        var updatedItem = new WatchedItem { ItemId = 1234, TargetBuyPrice = 1000, TargetSellPrice = 1500 };
        repository.AddOrUpdateItem(updatedItem);

        var items = repository.GetAllWatchedItems();
        Assert.Single(items);
        Assert.Equal(1000u, items.First().TargetBuyPrice);
        Assert.True(items.First().IsSellWatchEnabled);
        configService.Received(2).Save();
    }

    [Fact]
    public void RemoveItem_ShouldRemoveItemAndSave_WhenItemExists() {
        var configService = this.CreateMockConfigService();
        var repository = new MarketWatchRepository(configService);
        repository.AddOrUpdateItem(new WatchedItem { ItemId = 1234 });

        repository.RemoveItem(1234);

        var items = repository.GetAllWatchedItems();
        Assert.Empty(items);
        configService.Received(2).Save();
    }

    [Fact]
    public void Constructor_ShouldLoadExistingItems_FromConfiguration() {
        var configService = Substitute.For<IConfigurationService>();
        var config = new PluginConfiguration();
        config.WatchedItems[9999] = new WatchedItem { ItemId = 9999, TargetBuyPrice = 123 };
        configService.GetConfig().Returns(config);

        var repository = new MarketWatchRepository(configService);

        var items = repository.GetAllWatchedItems();
        Assert.Single(items);
        Assert.Equal(9999u, items.First().ItemId);
    }

    [Fact]
    public void IsEligibleForPolling_ShouldReturnTrue_OnlyWhenConfiguredProperly() {
        var item = new WatchedItem { ItemId = 1234 };
        Assert.False(item.IsEligibleForPolling());

        item.TargetBuyPrice = 0;
        Assert.False(item.IsEligibleForPolling());

        item.TargetBuyPrice = 100;
        Assert.True(item.IsEligibleForPolling());
    }
}