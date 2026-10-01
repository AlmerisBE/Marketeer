using Marketeer.API.Universalis.Models;
using Marketeer.Core.CompetitionTracking.Contracts;
using Marketeer.Core.CompetitionTracking.Models;
using Marketeer.Core.CompetitionTracking.Services;
using Marketeer.Core.Configuration.Contracts;
using Marketeer.Core.Configuration.Models;
using Marketeer.Core.MarketPricing.Models;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Core.CompetitionTracking.Services;

public class CompetitionEvaluatorServiceTests {
    [Fact]
    public void TryEvaluateListing_WithIgnoreBehavior_IgnoresWhitelistedCompetitor() {
        var configService = Substitute.For<IConfigurationService>();
        var whitelistManager = Substitute.For<IWhitelistManagerService>();

        configService.GetConfig().Returns(new PluginConfiguration { CompetitorWhitelistBehavior = WhitelistBehavior.Ignore });
        whitelistManager.IsWhitelisted("FriendlyRetainer").Returns(true);

        var service = new CompetitionEvaluatorService(configService, whitelistManager);

        var listing = new RetainerListing { ItemId = 1, CurrentPrice = 5000, IsHq = false };
        var pricing = new MarketItemPricing {
            ItemId = 1,
            Listings = new List<LowestPriceResult> {
                new LowestPriceResult { Price = 4000, RetainerName = "FriendlyRetainer", IsHq = false }, // Should be ignored
                new LowestPriceResult { Price = 6000, RetainerName = "RandomGuy", IsHq = false } // More expensive than us
            }
        };

        bool result = service.TryEvaluateListing(listing, pricing, "Player", "Test Item", out var undercut);

        // Assert that the friendly retainer was completely ignored, and we are not undercut by RandomGuy
        Assert.False(result);
        Assert.Null(undercut);
    }

    [Fact]
    public void TryEvaluateListing_WithMatchPriceBehavior_MatchesWhitelistedCompetitorPrice() {
        var configService = Substitute.For<IConfigurationService>();
        var whitelistManager = Substitute.For<IWhitelistManagerService>();

        configService.GetConfig().Returns(new PluginConfiguration { CompetitorWhitelistBehavior = WhitelistBehavior.MatchPrice });
        whitelistManager.IsWhitelisted("FriendlyRetainer").Returns(true);

        var service = new CompetitionEvaluatorService(configService, whitelistManager);

        var listing = new RetainerListing { ItemId = 1, CurrentPrice = 5000, IsHq = false };
        var pricing = new MarketItemPricing {
            ItemId = 1,
            Listings = new List<LowestPriceResult> {
                new LowestPriceResult { Price = 4000, RetainerName = "FriendlyRetainer", IsHq = false }
            }
        };

        bool result = service.TryEvaluateListing(listing, pricing, "Player", "Test Item", out var undercut);

        // Assert that we flag the undercut, but target the exact same price instead of Price - 1
        Assert.True(result);
        Assert.NotNull(undercut);
        Assert.Equal(4000u, undercut.TargetPrice);
    }
}