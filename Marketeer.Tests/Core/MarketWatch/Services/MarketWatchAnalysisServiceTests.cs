using Marketeer.API.Universalis.Models;
using Marketeer.Core.Logging.Contracts;
using Marketeer.Core.MarketPricing.Contracts;
using Marketeer.Core.MarketPricing.Models;
using Marketeer.Core.MarketWatch.Contracts;
using Marketeer.Core.MarketWatch.Models;
using Marketeer.Core.MarketWatch.Services;
using Marketeer.Core.SalesHistory.Contracts;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Core.MarketWatch.Services;

public class MarketWatchAnalysisServiceTests {
    [Fact]
    public async Task AnalyzeMarketAsync_WhenBuyTargetReached_ReturnsAlert() {
        var repo = Substitute.For<IMarketWatchRepository>();
        var priceCache = Substitute.For<IMarketPriceCacheService>();
        var playerContext = Substitute.For<IMarketWatchPlayerContext>();
        var resolver = Substitute.For<IItemResolverService>();
        var logger = Substitute.For<ILoggerService>();

        playerContext.IsPlayerAvailable().Returns(true);
        playerContext.GetCurrentWorldId().Returns(73u);

        var watchedItem = new WatchedItem { ItemId = 123, TargetBuyPrice = 1000, IsHighQuality = false };
        repo.GetAllWatchedItems().Returns(new List<WatchedItem> { watchedItem });

        var pricing = new MarketItemPricing {
            ItemId = 123,
            Listings = new List<LowestPriceResult> { new LowestPriceResult { Price = 800, IsHq = false, RetainerName = "Seller" } }
        };

        priceCache.GetPricingsAsync(Arg.Any<IEnumerable<uint>>(), 73u, false)
                  .Returns(new List<MarketItemPricing> { pricing });

        resolver.ResolveItemName(123u).Returns("TestItem");

        var service = new MarketWatchAnalysisService(repo, priceCache, playerContext, resolver, logger);
        var alerts = await service.AnalyzeMarketAsync();

        Assert.Single(alerts);
        Assert.Equal(MarketWatchAlertType.BuyTargetReached, alerts[0].AlertType);
        Assert.Equal(800u, alerts[0].CurrentPrice);
    }
}