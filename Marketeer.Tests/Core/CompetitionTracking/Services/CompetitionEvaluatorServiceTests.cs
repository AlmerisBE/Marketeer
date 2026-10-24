using Marketeer.API.Universalis.Models;
using Marketeer.Core.CompetitionTracking.Models;
using Marketeer.Core.CompetitionTracking.Services;
using Marketeer.Core.MarketPricing.Contracts;
using Marketeer.Core.MarketPricing.Models;
using Marketeer.Core.MarketStrategy.Contracts;
using Marketeer.Core.MarketStrategy.Models;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Core.CompetitionTracking.Services;

public class CompetitionEvaluatorServiceTests {
    private IPriceCalculationService priceCalculationService;
    private IMarketAnomalyDetector anomalyDetector;

    public CompetitionEvaluatorServiceTests() {
        this.priceCalculationService = Substitute.For<IPriceCalculationService>();
        this.anomalyDetector = Substitute.For<IMarketAnomalyDetector>();
    }

    private CompetitionEvaluatorService CreateService() {
        return new CompetitionEvaluatorService(this.priceCalculationService, this.anomalyDetector);
    }

    [Fact]
    public void TryEvaluateListing_WithEmptyListings_ReturnsFalse() {
        var service = this.CreateService();
        var listing = new RetainerListing { ItemId = 1, CurrentPrice = 1000 };
        var pricing = new MarketItemPricing { ItemId = 1, Listings = new List<LowestPriceResult>() };

        bool result = service.TryEvaluateListing(listing, pricing, "Player", 1, "Item", out var undercut);

        Assert.False(result);
        Assert.Null(undercut);
    }

    [Fact]
    public void TryEvaluateListing_WhenAlreadyLowest_ReturnsFalse() {
        var service = this.CreateService();
        var listing = new RetainerListing { ItemId = 1, CurrentPrice = 1000 };
        var pricing = new MarketItemPricing { ItemId = 1, Listings = new List<LowestPriceResult> { new LowestPriceResult { Price = 1000 } } };

        this.priceCalculationService.CalculateTargetPrice(1, 1000, pricing, false)
            .Returns(new PriceCalculationResult { Action = PricingAction.KeepPrice, Reason = PricingReason.AlreadyLowest });

        bool result = service.TryEvaluateListing(listing, pricing, "Player", 1, "Item", out var undercut);

        Assert.False(result);
        Assert.Null(undercut);
    }

    [Fact]
    public void TryEvaluateListing_WhenUndercutNeeded_ReturnsTrueAndMapsData() {
        var service = this.CreateService();
        var listing = new RetainerListing { ItemId = 1, CurrentPrice = 2000, Quantity = 5, RetainerName = "MyRetainer", SlotIndex = 0 };
        var pricing = new MarketItemPricing { ItemId = 1, AverageSalePrice = 1500, Listings = new List<LowestPriceResult> { new LowestPriceResult { Price = 1500 } } };

        this.priceCalculationService.CalculateTargetPrice(1, 2000, pricing, false)
            .Returns(new PriceCalculationResult { Action = PricingAction.DecreasePrice, CalculatedPrice = 1499, CompetitorPrice = 1500, ContextData = "Rival" });

        bool result = service.TryEvaluateListing(listing, pricing, "Player", 1, "Item", out var undercut);

        Assert.True(result);
        Assert.NotNull(undercut);
        Assert.Equal(1499u, undercut.TargetPrice);
        Assert.Equal(1500u, undercut.ServerCheapestPrice);
        Assert.Equal("Rival", undercut.CompetitorName);
        Assert.Equal(PricingAction.DecreasePrice, undercut.SuggestedAction);
    }

    [Fact]
    public void TryEvaluateListing_WithMarketCrash_MapsAnomalyData() {
        var service = this.CreateService();
        var listing = new RetainerListing { ItemId = 1, CurrentPrice = 5000 };
        var pricing = new MarketItemPricing { ItemId = 1, Listings = new List<LowestPriceResult> { new LowestPriceResult { Price = 100 } } };
        var anomaly = new AnomalyReport { IsAnomalyDetected = true };

        this.priceCalculationService.CalculateTargetPrice(1, 5000, pricing, false)
            .Returns(new PriceCalculationResult { Action = PricingAction.KeepPrice, Reason = PricingReason.MarketCrashProtection, CalculatedPrice = 5000 });

        this.anomalyDetector.EvaluateMarket(pricing, false).Returns(anomaly);

        bool result = service.TryEvaluateListing(listing, pricing, "Player", 1, "Item", out var undercut);

        Assert.True(result);
        Assert.NotNull(undercut);
        Assert.Equal(PricingAction.KeepPrice, undercut.SuggestedAction);
        Assert.NotNull(undercut.AnomalyData);
        Assert.True(undercut.AnomalyData.IsAnomalyDetected);
    }
}