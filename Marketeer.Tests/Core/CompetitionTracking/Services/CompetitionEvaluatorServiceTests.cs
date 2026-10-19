using Marketeer.API.Universalis.Models;
using Marketeer.Core.CompetitionTracking.Contracts;
using Marketeer.Core.CompetitionTracking.Models;
using Marketeer.Core.CompetitionTracking.Services;
using Marketeer.Core.Configuration.Contracts;
using Marketeer.Core.Configuration.Models;
using Marketeer.Core.MarketPricing.Models;
using Marketeer.Core.MarketStrategy.Contracts;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Core.CompetitionTracking.Services;

public class CompetitionEvaluatorServiceTests {
    private IConfigurationService configService;
    private IWhitelistManagerService whitelistManager;
    private IMarketAnomalyDetector anomalyDetector;
    private ICompetitionRuleProvider ruleProvider;

    public CompetitionEvaluatorServiceTests() {
        this.configService = Substitute.For<IConfigurationService>();
        this.whitelistManager = Substitute.For<IWhitelistManagerService>();
        this.anomalyDetector = Substitute.For<IMarketAnomalyDetector>();
        this.ruleProvider = Substitute.For<ICompetitionRuleProvider>();
    }

    [Fact]
    public void TryEvaluateListing_HqVsHq_UndercutsLowestHq() {
        this.configService.GetConfig().Returns(new PluginConfiguration());
        this.ruleProvider.GetRuleForItem(1).Returns(new PricingRule { UndercutMode = UndercutMode.Absolute, UndercutAmount = 1 });

        var service = new CompetitionEvaluatorService(this.configService, this.whitelistManager, this.anomalyDetector, this.ruleProvider);

        var listing = new RetainerListing { ItemId = 1, CurrentPrice = 5000, IsHq = true, RetainerName = "Me" };
        var pricing = new MarketItemPricing {
            ItemId = 1,
            Listings = new List<LowestPriceResult> {
                new LowestPriceResult { Price = 2000, RetainerName = "CheapNQ", IsHq = false },
                new LowestPriceResult { Price = 4000, RetainerName = "TargetHQ", IsHq = true }
            }
        };

        bool result = service.TryEvaluateListing(listing, pricing, "PlayerName", 33u, "Test Item", out var undercut);

        Assert.True(result);
        Assert.Equal(3999u, undercut!.TargetPrice);
        Assert.Equal("TargetHQ", undercut.CompetitorName);
    }

    [Fact]
    public void TryEvaluateListing_HqVsNqWithPremium_AppliesPremium() {
        this.configService.GetConfig().Returns(new PluginConfiguration());
        this.ruleProvider.GetRuleForItem(1).Returns(new PricingRule { HqPremiumPercentage = 20.0, UndercutMode = UndercutMode.Absolute, UndercutAmount = 1 });

        var service = new CompetitionEvaluatorService(this.configService, this.whitelistManager, this.anomalyDetector, this.ruleProvider);

        var listing = new RetainerListing { ItemId = 1, CurrentPrice = 5000, IsHq = true, RetainerName = "Me" };
        var pricing = new MarketItemPricing {
            ItemId = 1,
            Listings = new List<LowestPriceResult> {
                new LowestPriceResult { Price = 1000, RetainerName = "CheapNQ", IsHq = false }
            }
        };

        bool result = service.TryEvaluateListing(listing, pricing, "PlayerName", 33u, "Test Item", out var undercut);

        Assert.True(result);
        Assert.Equal(1200u, undercut!.TargetPrice); // 1000 + 20%
        Assert.Equal("CheapNQ", undercut.CompetitorName);
    }

    [Fact]
    public void TryEvaluateListing_WithIgnoreHq_UndercutsLowestRegardlessOfQuality() {
        this.configService.GetConfig().Returns(new PluginConfiguration());
        this.ruleProvider.GetRuleForItem(1).Returns(new PricingRule { IgnoreHq = true, UndercutMode = UndercutMode.Absolute, UndercutAmount = 1 });

        var service = new CompetitionEvaluatorService(this.configService, this.whitelistManager, this.anomalyDetector, this.ruleProvider);

        var listing = new RetainerListing { ItemId = 1, CurrentPrice = 5000, IsHq = true, RetainerName = "Me" };
        var pricing = new MarketItemPricing {
            ItemId = 1,
            Listings = new List<LowestPriceResult> {
                new LowestPriceResult { Price = 2000, RetainerName = "CheapNQ", IsHq = false },
                new LowestPriceResult { Price = 4000, RetainerName = "ExpensiveHQ", IsHq = true }
            }
        };

        bool result = service.TryEvaluateListing(listing, pricing, "PlayerName", 33u, "Test Item", out var undercut);

        Assert.True(result);
        Assert.Equal(1999u, undercut!.TargetPrice);
        Assert.Equal("CheapNQ", undercut.CompetitorName);
    }

    [Fact]
    public void TryEvaluateListing_AutoHealsMarket_WhenOurPriceIsCheaperThanTarget() {
        this.configService.GetConfig().Returns(new PluginConfiguration());
        this.ruleProvider.GetRuleForItem(1).Returns(new PricingRule { UndercutMode = UndercutMode.Absolute, UndercutAmount = 1 });

        var service = new CompetitionEvaluatorService(this.configService, this.whitelistManager, this.anomalyDetector, this.ruleProvider);

        var listing = new RetainerListing { ItemId = 1, CurrentPrice = 100, IsHq = false, RetainerName = "Me" };
        var pricing = new MarketItemPricing {
            ItemId = 1,
            Listings = new List<LowestPriceResult> {
                new LowestPriceResult { Price = 5000, RetainerName = "Competitor", IsHq = false }
            }
        };

        bool result = service.TryEvaluateListing(listing, pricing, "PlayerName", 33u, "Test Item", out var undercut);

        Assert.True(result);
        Assert.Equal(4999u, undercut!.TargetPrice); // Raised price from 100 to 4999!
    }
}