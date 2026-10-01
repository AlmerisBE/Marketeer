using Marketeer.API.Universalis.Models;
using Marketeer.Core.Configuration.Contracts;
using Marketeer.Core.Configuration.Models;
using Marketeer.Core.MarketPricing.Models;
using Marketeer.Core.MarketStrategy.Services;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Core.MarketStrategy.Services;

public class MarketAnomalyDetectorServiceTests {
    [Fact]
    public void EvaluateMarket_WhenPricesDropBelowThreshold_DetectsAnomalyAndCalculatesArbitrage() {
        var configService = Substitute.For<IConfigurationService>();
        var config = new PluginConfiguration {
            EnableAnomalyProtection = true,
            AnomalyCrashThreshold = 0.5
        };
        configService.GetConfig().Returns(config);

        var service = new MarketAnomalyDetectorService(configService);

        var pricing = new MarketItemPricing {
            ItemId = 123,
            AverageSalePrice = 10000, // True Market Value based on Universalis
            Listings = new List<LowestPriceResult> {
                new LowestPriceResult { Price = 1000, IsHq = false }, // Dumped
                new LowestPriceResult { Price = 2500, IsHq = false }, // Dumped
                new LowestPriceResult { Price = 9000, IsHq = false }, // Normal competition
                new LowestPriceResult { Price = 9500, IsHq = false }  // Normal competition
            }
        };

        // Act
        var report = service.EvaluateMarket(pricing, false);

        // Assert
        Assert.True(report.IsAnomalyDetected);
        Assert.Equal(10000u, report.TrueMarketValue);
        Assert.Equal(2, report.DumpedItemCount);
        Assert.Equal(3500u, report.TotalBuyoutCost); // 1000 + 2500
        Assert.Equal(16500u, report.PotentialGrossProfit); // (10000 * 2) - 3500
    }

    [Fact]
    public void EvaluateMarket_WhenNoAverageSalePriceIsAvailable_ReturnsNoAnomaly() {
        var configService = Substitute.For<IConfigurationService>();
        configService.GetConfig().Returns(new PluginConfiguration { EnableAnomalyProtection = true });

        var service = new MarketAnomalyDetectorService(configService);

        var pricing = new MarketItemPricing {
            ItemId = 123,
            AverageSalePrice = 0, // Missing historical data
            Listings = new List<LowestPriceResult> {
                new LowestPriceResult { Price = 100, IsHq = false }
            }
        };

        // Act
        var report = service.EvaluateMarket(pricing, false);

        // Assert
        Assert.False(report.IsAnomalyDetected);
    }
}