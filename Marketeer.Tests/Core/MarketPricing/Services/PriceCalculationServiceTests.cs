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
    public void CalculateTargetPrice_WithRelativeUndercut_ShouldApplyPercentage() {
        var configService = Substitute.For<IConfigurationService>();
        var itemResolver = Substitute.For<IItemResolverService>();

        var config = new PluginConfiguration {
            UndercutMode = UndercutMode.Relative,
            UndercutRelativePercentage = 5.0, // 5% undercut
            EnforceVendorPriceMinimum = false
        };
        configService.GetConfig().Returns(config);

        var service = new PriceCalculationService(configService, itemResolver);

        var marketListings = new List<LowestPriceResult> {
            new LowestPriceResult { Price = 1000, RetainerName = "Competitor" }
        };

        var result = service.CalculateTargetPrice(1, 1500, marketListings);

        Assert.Equal(PricingAction.UpdatePrice, result.Action);
        Assert.Equal(950u, result.CalculatedPrice);
    }

    [Fact]
    public void CalculateTargetPrice_WhenWhitelistIgnored_ShouldUndercutSecondCompetitor() {
        var configService = Substitute.For<IConfigurationService>();
        var itemResolver = Substitute.For<IItemResolverService>();

        var config = new PluginConfiguration {
            UndercutMode = UndercutMode.Absolute,
            UndercutAmount = 1,
            CompetitorWhitelist = new List<string> { "Friend" },
            CompetitorWhitelistBehavior = WhitelistBehavior.Ignore,
            EnforceVendorPriceMinimum = false
        };
        configService.GetConfig().Returns(config);

        var service = new PriceCalculationService(configService, itemResolver);

        var marketListings = new List<LowestPriceResult> {
            new LowestPriceResult { Price = 500, RetainerName = "Friend" },
            new LowestPriceResult { Price = 600, RetainerName = "Stranger" }
        };

        var result = service.CalculateTargetPrice(1, 1000, marketListings);

        Assert.Equal(PricingAction.UpdatePrice, result.Action);
        Assert.Equal(599u, result.CalculatedPrice); // Undercuts "Stranger", ignores "Friend"
    }

    [Fact]
    public void CalculateTargetPrice_WhenLossDetectedAndConfiguredToCancel_ShouldReturnCancelAction() {
        var configService = Substitute.For<IConfigurationService>();
        var itemResolver = Substitute.For<IItemResolverService>();

        var config = new PluginConfiguration {
            UndercutMode = UndercutMode.Absolute,
            UndercutAmount = 1,
            EnforceVendorPriceMinimum = true,
            LossBehavior = MinimumPriceBehavior.CancelToInventory
        };
        configService.GetConfig().Returns(config);
        itemResolver.ResolveVendorPrice(1).Returns(100u); // Vendor price is 100

        var service = new PriceCalculationService(configService, itemResolver);

        var marketListings = new List<LowestPriceResult> {
            new LowestPriceResult { Price = 90, RetainerName = "Competitor" } // Target will be 89
        };

        var result = service.CalculateTargetPrice(1, 150, marketListings);

        Assert.Equal(PricingAction.CancelListing, result.Action);
    }

    [Fact]
    public void CalculateTargetPrice_WhenMarketEmpty_ShouldReturnFallbackAverage() {
        var configService = Substitute.For<IConfigurationService>();
        var itemResolver = Substitute.For<IItemResolverService>();

        var config = new PluginConfiguration {
            EmptyMarketFallbackMode = FallbackPricingMode.AverageListingPrice
        };
        configService.GetConfig().Returns(config);
        itemResolver.ResolveVendorPrice(1).Returns(100u);

        var service = new PriceCalculationService(configService, itemResolver);

        var marketListings = new List<LowestPriceResult> {
            new LowestPriceResult { Price = 1000, RetainerName = "MyRetainer" }, // Ignored as competitor, but used for average
            new LowestPriceResult { Price = 2000, RetainerName = "MyRetainer2" }
        };

        var result = service.CalculateTargetPrice(1, 0, marketListings);

        Assert.Equal(PricingAction.UpdatePrice, result.Action);
        Assert.Equal(1500u, result.CalculatedPrice); // Average of 1000 and 2000
    }
}