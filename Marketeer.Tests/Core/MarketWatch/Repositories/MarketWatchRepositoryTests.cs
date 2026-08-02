using Marketeer.API.MarketWatch.Models;
using Marketeer.Core.MarketWatch.Repositories;
using Xunit;

namespace Marketeer.Tests.Core.MarketWatch.Repositories;

public class MarketWatchRepositoryTests {
    [Fact]
    public void AddOrUpdateItem_ShouldAddNewItem_WhenItemDoesNotExist() {
        var repository = new MarketWatchRepository();
        var item = new WatchedItem { ItemId = 1234, TargetBuyPrice = 500 };

        repository.AddOrUpdateItem(item);

        var items = repository.GetAllWatchedItems();
        Assert.Single(items);
        Assert.Equal(1234u, items.First().ItemId);
        Assert.True(items.First().IsBuyWatchEnabled);
    }

    [Fact]
    public void AddOrUpdateItem_ShouldUpdateExistingItem_WhenItemAlreadyExists() {
        var repository = new MarketWatchRepository();
        repository.AddOrUpdateItem(new WatchedItem { ItemId = 1234, TargetBuyPrice = 500 });

        var updatedItem = new WatchedItem { ItemId = 1234, TargetBuyPrice = 1000, TargetSellPrice = 1500 };
        repository.AddOrUpdateItem(updatedItem);

        var items = repository.GetAllWatchedItems();
        Assert.Single(items);
        Assert.Equal(1000u, items.First().TargetBuyPrice);
        Assert.True(items.First().IsSellWatchEnabled);
    }

    [Fact]
    public void RemoveItem_ShouldRemoveItemFromList_WhenItemExists() {
        var repository = new MarketWatchRepository();
        repository.AddOrUpdateItem(new WatchedItem { ItemId = 1234 });
        repository.AddOrUpdateItem(new WatchedItem { ItemId = 5678 });

        repository.RemoveItem(1234);

        var items = repository.GetAllWatchedItems();
        Assert.Single(items);
        Assert.Equal(5678u, items.First().ItemId);
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