using Marketeer.API.Universalis.Models;
using Marketeer.Core.CompetitionTracking.Contracts;
using Marketeer.Core.CompetitionTracking.Models;
using Marketeer.Core.CompetitionTracking.Services;
using Marketeer.Core.Configuration.Contracts;
using Marketeer.Core.Configuration.Models;
using Marketeer.Core.MarketPricing.Models;
using Marketeer.Core.MarketStrategy.Contracts;
using Marketeer.Core.MarketStrategy.Models;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Core.CompetitionTracking.Services;

public class CompetitionEvaluatorServiceTests {
    [Fact]
    public void TryEvaluateListing_WhenCompetitorIsWhitelisted_ReturnsFalseAndIgnoresUndercut() {
        var configService = Substitute.For<IConfigurationService>();
        var whitelistManager = Substitute.For<IWhitelistManagerService>();
        var anomalyDetector = Substitute.For<IMarketAnomalyDetector>();

        configService.GetConfig().Returns(new PluginConfiguration { CompetitorWhitelistBehavior = WhitelistBehavior.Ignore });
        whitelistManager.IsWhitelisted("FriendlyRetainer").Returns(true);
        anomalyDetector.EvaluateMarket(Arg.Any<MarketItemPricing>(), Arg.Any<bool>()).Returns(new AnomalyReport());

        var service = new CompetitionEvaluatorService(configService, whitelistManager, anomalyDetector);

        var listing = new RetainerListing { ItemId = 1, CurrentPrice = 5000, IsHq = false };
        var pricing = new MarketItemPricing {
            ItemId = 1,
            Listings = new List<LowestPriceResult> {
                new LowestPriceResult { Price = 4000, RetainerName = "FriendlyRetainer", IsHq = false },
                new LowestPriceResult { Price = 6000, RetainerName = "RandomGuy", IsHq = false }
            }
        };

        bool result = service.TryEvaluateListing(listing, pricing, "Player", "Test Item", out var undercut);

        Assert.False(result);
        Assert.Null(undercut);
    }

    [Fact]
    public void TryEvaluateListing_WithIgnoreBehavior_IgnoresWhitelistedCompetitor() {
        var configService = Substitute.For<IConfigurationService>();
        var whitelistManager = Substitute.For<IWhitelistManagerService>();
        var anomalyDetector = Substitute.For<IMarketAnomalyDetector>();

        configService.GetConfig().Returns(new PluginConfiguration { CompetitorWhitelistBehavior = WhitelistBehavior.Ignore });
        whitelistManager.IsWhitelisted("FriendlyRetainer").Returns(true);
        anomalyDetector.EvaluateMarket(Arg.Any<MarketItemPricing>(), Arg.Any<bool>()).Returns(new AnomalyReport());

        var service = new CompetitionEvaluatorService(configService, whitelistManager, anomalyDetector);

        var listing = new RetainerListing { ItemId = 1, CurrentPrice = 5000, IsHq = false };
        var pricing = new MarketItemPricing {
            ItemId = 1,
            Listings = new List<LowestPriceResult> {
                new LowestPriceResult { Price = 4000, RetainerName = "FriendlyRetainer", IsHq = false },
                new LowestPriceResult { Price = 6000, RetainerName = "RandomGuy", IsHq = false }
            }
        };

        bool result = service.TryEvaluateListing(listing, pricing, "Player", "Test Item", out var undercut);

        Assert.False(result);
        Assert.Null(undercut);
    }

    [Fact]
    public void TryEvaluateListing_WithMatchPriceBehavior_MatchesWhitelistedCompetitorPrice() {
        var configService = Substitute.For<IConfigurationService>();
        var whitelistManager = Substitute.For<IWhitelistManagerService>();
        var anomalyDetector = Substitute.For<IMarketAnomalyDetector>();

        configService.GetConfig().Returns(new PluginConfiguration { CompetitorWhitelistBehavior = WhitelistBehavior.MatchPrice });
        whitelistManager.IsWhitelisted("FriendlyRetainer").Returns(true);
        anomalyDetector.EvaluateMarket(Arg.Any<MarketItemPricing>(), Arg.Any<bool>()).Returns(new AnomalyReport());

        var service = new CompetitionEvaluatorService(configService, whitelistManager, anomalyDetector);

        var listing = new RetainerListing { ItemId = 1, CurrentPrice = 5000, IsHq = false };
        var pricing = new MarketItemPricing {
            ItemId = 1,
            Listings = new List<LowestPriceResult> {
                new LowestPriceResult { Price = 4000, RetainerName = "FriendlyRetainer", IsHq = false }
            }
        };

        bool result = service.TryEvaluateListing(listing, pricing, "Player", "Test Item", out var undercut);

        Assert.True(result);
        Assert.NotNull(undercut);
        Assert.Equal(4000u, undercut.TargetPrice);
    }

    [Fact]
    public void TryEvaluateListing_WithHoldPriceStrategy_ReturnsKeepPriceActionOnAnomaly() {
        var configService = Substitute.For<IConfigurationService>();
        var whitelistManager = Substitute.For<IWhitelistManagerService>();
        var anomalyDetector = Substitute.For<IMarketAnomalyDetector>();

        configService.GetConfig().Returns(new PluginConfiguration {
            CompetitorWhitelistBehavior = WhitelistBehavior.Ignore,
            AnomalyStrategy = AnomalyDefenseStrategy.HoldPrice
        });

        var anomalyReport = new AnomalyReport { IsAnomalyDetected = true };
        anomalyDetector.EvaluateMarket(Arg.Any<MarketItemPricing>(), Arg.Any<bool>()).Returns(anomalyReport);

        var service = new CompetitionEvaluatorService(configService, whitelistManager, anomalyDetector);

        var listing = new RetainerListing { ItemId = 1, CurrentPrice = 10000, IsHq = false };
        var pricing = new MarketItemPricing {
            ItemId = 1,
            Listings = new List<LowestPriceResult> {
                new LowestPriceResult { Price = 1000, RetainerName = "CrashingGuy", IsHq = false }
            }
        };

        bool result = service.TryEvaluateListing(listing, pricing, "Player", "Test Item", out var undercut);

        Assert.True(result);
        Assert.NotNull(undercut);
        Assert.Equal(PricingAction.KeepPrice, undercut.SuggestedAction);
        Assert.Equal(10000u, undercut.TargetPrice);
        Assert.NotNull(undercut.AnomalyData);
    }

    [Fact]
    public void TryEvaluateListing_WhenUndercutDetected_MapsAverageMarketPriceCorrectly() {
        var configService = Substitute.For<IConfigurationService>();
        var whitelistManager = Substitute.For<IWhitelistManagerService>();
        var anomalyDetector = Substitute.For<IMarketAnomalyDetector>();

        configService.GetConfig().Returns(new PluginConfiguration { CompetitorWhitelistBehavior = WhitelistBehavior.Ignore });
        anomalyDetector.EvaluateMarket(Arg.Any<MarketItemPricing>(), Arg.Any<bool>()).Returns(new AnomalyReport());

        var service = new CompetitionEvaluatorService(configService, whitelistManager, anomalyDetector);

        var listing = new RetainerListing { ItemId = 1, CurrentPrice = 10000, IsHq = false };
        var pricing = new MarketItemPricing {
            ItemId = 1,
            AverageSalePrice = 8500, // Simulated True Market Value
            Listings = new List<LowestPriceResult> {
                new LowestPriceResult { Price = 9000, RetainerName = "Rival", IsHq = false }
            }
        };

        bool result = service.TryEvaluateListing(listing, pricing, "Player", "Test Item", out var undercut);

        Assert.True(result);
        Assert.NotNull(undercut);

        // Assert that the UI model successfully received the TMV from the pricing entity
        Assert.Equal(8500u, undercut.AverageMarketPrice);
    }

    [Fact]
    public void TryEvaluateListing_WithUndercutNormalMarketStrategy_IgnoresDumpedListingsAndTargetsNormalPrice() {
        var configService = Substitute.For<IConfigurationService>();
        var whitelistManager = Substitute.For<IWhitelistManagerService>();
        var anomalyDetector = Substitute.For<IMarketAnomalyDetector>();

        configService.GetConfig().Returns(new PluginConfiguration {
            CompetitorWhitelistBehavior = WhitelistBehavior.Ignore,
            AnomalyStrategy = AnomalyDefenseStrategy.UndercutNormalMarket
        });

        // Simulate a market crash with a threshold of 5000
        var anomalyReport = new AnomalyReport {
            IsAnomalyDetected = true,
            CrashThresholdPrice = 5000
        };
        anomalyDetector.EvaluateMarket(Arg.Any<MarketItemPricing>(), Arg.Any<bool>()).Returns(anomalyReport);

        var service = new CompetitionEvaluatorService(configService, whitelistManager, anomalyDetector);

        var listing = new RetainerListing { ItemId = 1, CurrentPrice = 10000, IsHq = false };
        var pricing = new MarketItemPricing {
            ItemId = 1,
            AverageSalePrice = 10000,
            Listings = new List<LowestPriceResult> {
                new LowestPriceResult { Price = 1000, RetainerName = "CrashingGuy1", IsHq = false }, // Dumped
                new LowestPriceResult { Price = 2500, RetainerName = "CrashingGuy2", IsHq = false }, // Dumped
                new LowestPriceResult { Price = 9000, RetainerName = "NormalGuy", IsHq = false },    // First normal competitor
                new LowestPriceResult { Price = 9500, RetainerName = "OtherGuy", IsHq = false }
            }
        };

        bool result = service.TryEvaluateListing(listing, pricing, "Player", "Test Item", out var undercut);

        Assert.True(result);
        Assert.NotNull(undercut);

        // Assert that the dumped listings were ignored and we target the first normal price (9000 - 1)
        Assert.Equal(PricingAction.UpdatePrice, undercut.SuggestedAction);
        Assert.Equal(8999u, undercut.TargetPrice);
        Assert.Equal("NormalGuy", undercut.CompetitorName);
    }
}