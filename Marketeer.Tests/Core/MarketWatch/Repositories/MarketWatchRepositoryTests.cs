using Marketeer.API.Configuration.Contracts;
using Marketeer.API.Configuration.Models;
using Marketeer.API.MarketWatch.Models;
using Marketeer.Core.MarketWatch.Repositories;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Core.MarketWatch.Repositories;

public class MarketWatchRepositoryTests {
    private IConfigurationService configService;
    private PluginConfiguration config;

    public MarketWatchRepositoryTests() {
        this.configService = Substitute.For<IConfigurationService>();
        this.config = new PluginConfiguration();
        this.configService.GetConfig().Returns(this.config);
    }

    [Fact]
    public void GetAllWatchedItems_ShouldReturnAllItems() {
        this.config.WatchedItems = new Dictionary<string, WatchedItem> {
            { "1_NQ", new WatchedItem { ItemId = 1, IsHighQuality = false } },
            { "1_HQ", new WatchedItem { ItemId = 1, IsHighQuality = true } }
        };

        var repository = new MarketWatchRepository(this.configService);
        var items = repository.GetAllWatchedItems();

        Assert.Equal(2, items.Count);
        Assert.Contains(items, i => i.ItemId == 1 && !i.IsHighQuality);
        Assert.Contains(items, i => i.ItemId == 1 && i.IsHighQuality);
    }

    [Fact]
    public void AddOrUpdateItem_ShouldAddNewItem_WithCorrectCompositeKey() {
        var repository = new MarketWatchRepository(this.configService);
        var item = new WatchedItem { ItemId = 2, IsHighQuality = true };

        repository.AddOrUpdateItem(item);

        Assert.Single(this.config.WatchedItems);
        Assert.True(this.config.WatchedItems.ContainsKey("2_HQ"));
        this.configService.Received(1).Save();
    }

    [Fact]
    public void RemoveItem_ShouldRemoveItemAndSave_WhenItemExists() {
        this.config.WatchedItems = new Dictionary<string, WatchedItem> {
            { "3_NQ", new WatchedItem { ItemId = 3, IsHighQuality = false } },
            { "3_HQ", new WatchedItem { ItemId = 3, IsHighQuality = true } }
        };

        var repository = new MarketWatchRepository(this.configService);

        // Remove only the NQ variant
        repository.RemoveItem(3, false);

        Assert.Single(this.config.WatchedItems);
        Assert.True(this.config.WatchedItems.ContainsKey("3_HQ"));
        Assert.False(this.config.WatchedItems.ContainsKey("3_NQ"));
        this.configService.Received(1).Save();
    }

    [Fact]
    public void RemoveItem_ShouldNotSave_WhenItemDoesNotExist() {
        this.config.WatchedItems = new Dictionary<string, WatchedItem> {
            { "4_HQ", new WatchedItem { ItemId = 4, IsHighQuality = true } }
        };

        var repository = new MarketWatchRepository(this.configService);

        // Attempting to remove NQ variant which does not exist
        repository.RemoveItem(4, false);

        Assert.Single(this.config.WatchedItems);
        this.configService.DidNotReceive().Save();
    }
}