using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin.Services;
using Marketeer.API.Universalis.Contracts;
using Marketeer.API.Universalis.Models;
using Marketeer.Core.Logging.Contracts;
using Marketeer.Core.MarketWatch.Contracts;
using Marketeer.Core.MarketWatch.Models;
using Marketeer.Core.MarketWatch.Services;
using Marketeer.Core.SalesHistory.Contracts;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Core.MarketWatch.Services;

public class MarketWatchAnalysisServiceTests {
    private IMarketWatchRepository repository;
    private IServerPriceProvider priceProvider;
    private IObjectTable objectTable;
    private IItemResolverService itemResolver;
    private ILoggerService logger;
    private MarketWatchAnalysisService service;

    public MarketWatchAnalysisServiceTests() {
        this.repository = Substitute.For<IMarketWatchRepository>();
        this.priceProvider = Substitute.For<IServerPriceProvider>();
        this.objectTable = Substitute.For<IObjectTable>();
        this.itemResolver = Substitute.For<IItemResolverService>();
        this.logger = Substitute.For<ILoggerService>();

        var localPlayer = Substitute.For<IPlayerCharacter>();
        this.objectTable.Length.Returns(1);
        this.objectTable[0].Returns(localPlayer);
        this.itemResolver.ResolveItemName(Arg.Any<uint>()).Returns("Test Item");

        this.service = new MarketWatchAnalysisService(this.repository, this.priceProvider, this.objectTable, this.itemResolver, this.logger);
    }

    [Fact]
    public async Task NQBuy_ShouldTrigger_WhenHqIsCheaper() {
        // NQ tracked for Buy at 1000. Market has NQ at 1200, HQ at 800. 
        // Expect: Alert generated for the HQ item at 800 (Rule 1).
        this.repository.GetAllWatchedItems().Returns(new List<WatchedItem> { new WatchedItem { ItemId = 1, TargetBuyPrice = 1000, IsHighQuality = false } });
        this.priceProvider.GetLowestPricesAsync(Arg.Any<IEnumerable<uint>>(), 0).Returns(new List<LowestPriceResult> {
            new LowestPriceResult { ItemId = 1, Price = 1200, IsHq = false },
            new LowestPriceResult { ItemId = 1, Price = 800, IsHq = true }
        });

        var alerts = await this.service.AnalyzeMarketAsync();

        Assert.Single(alerts);
        Assert.Equal(800u, alerts.First().CurrentPrice);
        Assert.True(alerts.First().IsHighQuality);
    }

    [Fact]
    public async Task HQBuy_ShouldNotTrigger_WhenNqIsCheaper() {
        // HQ tracked for Buy at 1000. Market has NQ at 800, HQ at 1500.
        // Expect: No alert, because the buyer explicitly wants HQ and it's too expensive (Rule 2).
        this.repository.GetAllWatchedItems().Returns(new List<WatchedItem> { new WatchedItem { ItemId = 2, TargetBuyPrice = 1000, IsHighQuality = true } });
        this.priceProvider.GetLowestPricesAsync(Arg.Any<IEnumerable<uint>>(), 0).Returns(new List<LowestPriceResult> {
            new LowestPriceResult { ItemId = 2, Price = 800, IsHq = false },
            new LowestPriceResult { ItemId = 2, Price = 1500, IsHq = true }
        });

        var alerts = await this.service.AnalyzeMarketAsync();

        Assert.Empty(alerts);
    }

    [Fact]
    public async Task NQSell_ShouldTrigger_WhenHqIsCheap() {
        // NQ tracked for Sell at 1000. Market has NQ at 1500, HQ at 900.
        // Expect: No alert. The cheap HQ item prevents our NQ from selling at 1000. Effective market price is 900.
        this.repository.GetAllWatchedItems().Returns(new List<WatchedItem> { new WatchedItem { ItemId = 3, TargetSellPrice = 1000, IsHighQuality = false } });
        this.priceProvider.GetLowestPricesAsync(Arg.Any<IEnumerable<uint>>(), 0).Returns(new List<LowestPriceResult> {
            new LowestPriceResult { ItemId = 3, Price = 1500, IsHq = false },
            new LowestPriceResult { ItemId = 3, Price = 900, IsHq = true }
        });

        var alerts = await this.service.AnalyzeMarketAsync();

        Assert.Empty(alerts);
    }

    [Fact]
    public async Task HQSell_ShouldTrigger_EvenIfNqIsCheap() {
        // HQ tracked for Sell at 1000. Market has NQ at 500, HQ at 1200.
        // Expect: Alert generated for Sell. HQ buyers ignore NQ, so effective market price is 1200 (Rule 4).
        this.repository.GetAllWatchedItems().Returns(new List<WatchedItem> { new WatchedItem { ItemId = 4, TargetSellPrice = 1000, IsHighQuality = true } });
        this.priceProvider.GetLowestPricesAsync(Arg.Any<IEnumerable<uint>>(), 0).Returns(new List<LowestPriceResult> {
            new LowestPriceResult { ItemId = 4, Price = 500, IsHq = false },
            new LowestPriceResult { ItemId = 4, Price = 1200, IsHq = true }
        });

        var alerts = await this.service.AnalyzeMarketAsync();

        Assert.Single(alerts);
        Assert.Equal(1200u, alerts.First().CurrentPrice);
    }
}