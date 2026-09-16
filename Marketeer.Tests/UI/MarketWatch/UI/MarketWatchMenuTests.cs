using Marketeer.API.GameData.Contracts;
using Marketeer.API.GameData.Models;
using Marketeer.Core.MarketWatch.Contracts;
using Marketeer.Core.MarketWatch.Models;
using Marketeer.Core.SalesHistory.Contracts;
using Marketeer.UI.Localization.Contracts;
using Marketeer.UI.MarketWatch.UI;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.UI.MarketWatch.UI;

public class MarketWatchMenuTests {
    [Fact]
    public void AddSelectedItem_ShouldAddItemToRepository_WhenItemIsSelected() {
        var repository = Substitute.For<IMarketWatchRepository>();
        var localization = Substitute.For<ILocalizationService>();
        var itemResolver = Substitute.For<IItemResolverService>();
        var searchProvider = Substitute.For<IMarketItemSearchProvider>();

        var menu = new MarketWatchMenu(repository, localization, itemResolver, searchProvider);

        // Reflection is used here to set the private selectedItem field for test simulation
        var field = typeof(MarketWatchMenu).GetField("selectedItem", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(menu, new ItemSearchResult { ItemId = 1234, Name = "Test Item" });

        menu.AddSelectedItem();

        repository.Received(1).AddOrUpdateItem(Arg.Is<WatchedItem>(i => i.ItemId == 1234));
    }

    [Fact]
    public void AddSelectedItem_ShouldNotCallRepository_WhenNoItemIsSelected() {
        var repository = Substitute.For<IMarketWatchRepository>();
        var localization = Substitute.For<ILocalizationService>();
        var itemResolver = Substitute.For<IItemResolverService>();
        var searchProvider = Substitute.For<IMarketItemSearchProvider>();

        var menu = new MarketWatchMenu(repository, localization, itemResolver, searchProvider);

        menu.AddSelectedItem();

        repository.DidNotReceive().AddOrUpdateItem(Arg.Any<WatchedItem>());
    }
}