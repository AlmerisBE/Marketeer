using Dalamud.Plugin.Services;
using Marketeer.API.Universalis.Models;
using Marketeer.Core.CompetitionTracking.Contracts;
using Marketeer.Core.Logging.Contracts;
using Marketeer.Core.MarketPricing.Contracts;
using Marketeer.Core.MarketPricing.Models;
using Marketeer.Core.SalesHistory.Contracts;
using Marketeer.UI.CompetitionTracking.Commands;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.UI.CompetitionTracking.Commands;

public class PriceCommandTests {
    [Fact]
    public async Task ExecuteAsync_WhenItemFound_QueriesPriceAndPrintsToChat() {
        var priceCache = Substitute.For<IMarketPriceCacheService>();
        var itemResolver = Substitute.For<IItemResolverService>();
        var playerContext = Substitute.For<ICompetitionPlayerContext>();
        var chatGui = Substitute.For<IChatGui>();
        var logger = Substitute.For<ILoggerService>();
        var framework = Substitute.For<IFramework>();

        playerContext.IsPlayerAvailable().Returns(true);
        playerContext.GetCurrentWorldId().Returns(73u);

        itemResolver.ResolveItemId("TestItem").Returns(123u);

        var pricing = new MarketItemPricing {
            ItemId = 123u,
            Listings = new List<LowestPriceResult> {
                new LowestPriceResult { Price = 500, RetainerName = "TestRetainer" }
            },
            AverageSalePrice = 450
        };

        priceCache.GetPricingAsync(123u, 73u, false).Returns(pricing);

        framework.When(f => f.RunOnFrameworkThread(Arg.Any<System.Action>()))
                 .Do(x => x.Arg<System.Action>().Invoke());

        var command = new PriceCommand(priceCache, itemResolver, playerContext, chatGui, logger, framework);

        // Await the newly exposed async variant to prevent race conditions during testing
        await command.ExecuteAsync("TestItem");

        chatGui.Received().Print("[Marketeer] Lowest price for TestItem is 500g by TestRetainer.");
        chatGui.Received().Print("[Marketeer] Average Universalis historical sale price: 450g.");
    }
}