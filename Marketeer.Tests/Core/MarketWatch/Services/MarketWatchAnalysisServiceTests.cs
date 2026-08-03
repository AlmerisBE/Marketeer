using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin.Services;
using Marketeer.API.Logging.Contracts;
using Marketeer.API.MarketWatch.Contracts;
using Marketeer.API.MarketWatch.Models;
using Marketeer.API.SalesHistory.Contracts;
using Marketeer.API.Universalis.Contracts;
using Marketeer.API.Universalis.Models;
using Marketeer.Core.MarketWatch.Services;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Core.MarketWatch.Services;

public class MarketWatchAnalysisServiceTests {
    private IMarketWatchRepository repository;
    private IServerPriceProvider priceProvider;
    private IObjectTable objectTable;
    private IItemResolverService itemResolver;
    private ILoggerService logger;

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
    }

    [Fact]
    public async Task AnalyzeMarketAsync_ShouldReturnBuyAlert_WhenPriceIsLowerAndQualityMatches() {
        var watchedItems = new List<WatchedItem> {
            new WatchedItem { ItemId = 1, TargetBuyPrice = 1000, IsHighQuality = true }
        };
        this.repository.GetAllWatchedItems().Returns(watchedItems);

        var prices = new List<LowestPriceResult> {
            new LowestPriceResult { ItemId = 1, Price = 800, RetainerName = "SellerA", IsHq = true }
        };

        this.priceProvider.GetLowestPricesAsync(Arg.Any<IEnumerable<uint>>(), 0).Returns(prices);

        var service = new MarketWatchAnalysisService(this.repository, this.priceProvider, this.objectTable, this.itemResolver, this.logger);
        var alerts = await service.AnalyzeMarketAsync();

        Assert.Single(alerts);
        Assert.Equal(MarketWatchAlertType.BuyTargetReached, alerts.First().AlertType);
        Assert.True(alerts.First().IsHighQuality);
    }

    [Fact]
    public async Task AnalyzeMarketAsync_ShouldReturnNoAlerts_WhenPriceIsLowerButQualityMismatches() {
        var watchedItems = new List<WatchedItem> {
            new WatchedItem { ItemId = 2, TargetBuyPrice = 1000, IsHighQuality = true }
        };
        this.repository.GetAllWatchedItems().Returns(watchedItems);

        var prices = new List<LowestPriceResult> {
            new LowestPriceResult { ItemId = 2, Price = 800, RetainerName = "SellerB", IsHq = false }
        };

        this.priceProvider.GetLowestPricesAsync(Arg.Any<IEnumerable<uint>>(), 0).Returns(prices);

        var service = new MarketWatchAnalysisService(this.repository, this.priceProvider, this.objectTable, this.itemResolver, this.logger);
        var alerts = await service.AnalyzeMarketAsync();

        Assert.Empty(alerts);
    }
}