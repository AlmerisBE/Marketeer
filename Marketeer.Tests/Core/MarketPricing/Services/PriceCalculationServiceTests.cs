using Marketeer.API.Universalis.Models;
using Marketeer.Core.Configuration.Contracts;
using Marketeer.Core.Configuration.Models;
using Marketeer.Core.MarketPricing.Models;
using Marketeer.Core.MarketPricing.Services;
using Marketeer.Core.SalesHistory.Contracts;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Core.MarketPricing.Services;

public class PriceCalculationServiceTests {
    [Fact]
    public void CalculateTargetPrice_WhenUndercutAbsolute_ReturnsCorrectPrice() {
        var configService = Substitute.For<IConfigurationService>();
        var itemResolver = Substitute.For<IItemResolverService>();

        var config = new PluginConfiguration {
            UndercutMode = UndercutMode.Absolute,
            UndercutAmount = 2
        };
        configService.GetConfig().Returns(config);

        var service = new PriceCalculationService(configService, itemResolver);
        var pricing = new MarketItemPricing {
            ItemId = 123u,
            Listings = new List<LowestPriceResult> {
                new LowestPriceResult { Price = 1000, RetainerName = "Rival" }
            }
        };

        var result = service.CalculateTargetPrice(123u, 1500u, pricing);

        Assert.Equal(PricingAction.UpdatePrice, result.Action);
        Assert.Equal(998u, result.CalculatedPrice);
    }

    [Fact]
    public void CalculateTargetPrice_WhenUndercutRelative_ReturnsCorrectPrice() {
        var configService = Substitute.For<IConfigurationService>();
        var itemResolver = Substitute.For<IItemResolverService>();

        var config = new PluginConfiguration {
            UndercutMode = UndercutMode.Relative,
            UndercutRelativePercentage = 5.0
        };
        configService.GetConfig().Returns(config);

        var service = new PriceCalculationService(configService, itemResolver);
        var pricing = new MarketItemPricing {
            ItemId = 123u,
            Listings = new List<LowestPriceResult> {
                new LowestPriceResult { Price = 1000, RetainerName = "Rival" }
            }
        };

        var result = service.CalculateTargetPrice(123u, 1500u, pricing);

        Assert.Equal(PricingAction.UpdatePrice, result.Action);
        Assert.Equal(950u, result.CalculatedPrice); // 1000 - 5%
    }

    [Fact]
    public void CalculateTargetPrice_WhenRivalIsWhitelisted_ReturnsMatchedPrice() {
        var configService = Substitute.For<IConfigurationService>();
        var itemResolver = Substitute.For<IItemResolverService>();

        var config = new PluginConfiguration {
            CompetitorWhitelist = new List<string> { "FriendRetainer" },
            CompetitorWhitelistBehavior = WhitelistBehavior.MatchPrice
        };
        configService.GetConfig().Returns(config);

        var service = new PriceCalculationService(configService, itemResolver);
        var pricing = new MarketItemPricing {
            ItemId = 123u,
            Listings = new List<LowestPriceResult> {
                new LowestPriceResult { Price = 1000, RetainerName = "FriendRetainer" }
            }
        };

        var result = service.CalculateTargetPrice(123u, 1500u, pricing);

        Assert.Equal(PricingAction.UpdatePrice, result.Action);
        Assert.Equal(1000u, result.CalculatedPrice);
    }

    [Fact]
    public void CalculateTargetPrice_WhenMarketEmpty_ShouldReturnFallbackAverage() {
        var configService = Substitute.For<IConfigurationService>();
        var itemResolver = Substitute.For<IItemResolverService>();

        var config = new PluginConfiguration {
            EmptyMarketFallbackMode = FallbackPricingMode.AverageListingPrice,
            EmptyMarketFallbackMultiplier = 1.5
        };
        configService.GetConfig().Returns(config);

        itemResolver.ResolveVendorPrice(123u).Returns(1000u);

        var service = new PriceCalculationService(configService, itemResolver);

        var pricing = new MarketItemPricing {
            ItemId = 123u,
            Listings = new List<LowestPriceResult>(),
            AverageSalePrice = 2500u
        };

        var result = service.CalculateTargetPrice(123u, 0u, pricing);

        Assert.Equal(PricingAction.UpdatePrice, result.Action);
        Assert.Equal(2500u, result.CalculatedPrice);
    }
}