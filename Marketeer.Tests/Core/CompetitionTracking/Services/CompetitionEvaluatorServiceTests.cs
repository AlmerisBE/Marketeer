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
}