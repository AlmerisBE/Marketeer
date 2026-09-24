using Marketeer.API.GameData.Contracts;
using Marketeer.API.Universalis.Models;
using Marketeer.Core.CraftingProfit.Models;
using Marketeer.Core.CraftingProfit.Services;
using Marketeer.Core.Logging.Contracts;
using Marketeer.Core.MarketPricing.Contracts;
using Marketeer.Core.MarketPricing.Models;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Core.CraftingProfit.Services;

public class CraftingCostEvaluatorTests {
    [Fact]
    public async Task EvaluateAsync_CalculatesCostCorrectly() {
        var priceCache = Substitute.For<IMarketPriceCacheService>();
        var recipeData = Substitute.For<IRecipeDataService>();
        var logger = Substitute.For<ILoggerService>();

        var config = new CraftingItemConfig {
            ItemId = 100,
            TargetSellPrice = 5000,
            Components = new Dictionary<uint, ComponentConfig>()
        };

        var pricing = new MarketItemPricing {
            ItemId = 100,
            Listings = new List<LowestPriceResult> { new LowestPriceResult { Price = 4500 } }
        };

        priceCache.GetPricingAsync(100u, 73u, false).Returns(pricing);

        var service = new CraftingCostEvaluator(priceCache, recipeData, logger);
        var result = await service.EvaluateAsync(config, 73u);

        Assert.Equal(100u, result.ItemId);
        Assert.Equal(4500u, result.CurrentMarketPrice);
    }
}