using Marketeer.API.Universalis.Models;
using Marketeer.Core.Configuration.Contracts;
using Marketeer.Core.Configuration.Models;
using Marketeer.Core.MarketPricing.Contracts;
using Marketeer.Core.MarketPricing.Models;
using Marketeer.Core.MarketPricing.Services;
using Marketeer.Core.SalesHistory.Contracts;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Core.MarketPricing.Services;

public class PriceCalculationServiceTests {
    private IConfigurationService configService;
    private IItemResolverService itemResolver;
    private IPriceRuleProvider ruleProvider;
    private IPriceCrashProtector crashProtector;

    public PriceCalculationServiceTests() {
        this.configService = Substitute.For<IConfigurationService>();
        this.itemResolver = Substitute.For<IItemResolverService>();
        this.ruleProvider = Substitute.For<IPriceRuleProvider>();
        this.crashProtector = Substitute.For<IPriceCrashProtector>();
    }

    [Fact]
    public void CalculateTargetPrice_WithEmptyMarket_UsesFallback() {
        this.configService.GetConfig().Returns(new PluginConfiguration {
            EmptyMarketFallbackMode = FallbackPricingMode.VendorSellMultiple,
            EmptyMarketFallbackMultiplier = 2.0
        });
        this.ruleProvider.GetRuleForItem(Arg.Any<uint>()).Returns(new PricingRule());
        this.itemResolver.ResolveVendorPrice(1).Returns(1000u);

        var service = new PriceCalculationService(this.configService, this.itemResolver, this.ruleProvider, this.crashProtector);
        var pricing = new MarketItemPricing { ItemId = 1, Listings = new List<LowestPriceResult>() };

        var result = service.CalculateTargetPrice(1, 0, pricing, false);

        Assert.Equal(PricingAction.IncreasePrice, result.Action);
        Assert.Equal(2000u, result.CalculatedPrice);
        Assert.Equal(PricingReason.NoCompetition, result.Reason);
    }

    [Fact]
    public void CalculateTargetPrice_WithCompetitors_UndercutsAbsolute() {
        this.configService.GetConfig().Returns(new PluginConfiguration());
        this.ruleProvider.GetRuleForItem(1).Returns(new PricingRule { UndercutMode = UndercutMode.Absolute, UndercutAmount = 1 });

        var service = new PriceCalculationService(this.configService, this.itemResolver, this.ruleProvider, this.crashProtector);
        var pricing = new MarketItemPricing {
            ItemId = 1,
            Listings = new List<LowestPriceResult> {
                new LowestPriceResult { Price = 5000, RetainerName = "Competitor", IsHq = false }
            }
        };

        var result = service.CalculateTargetPrice(1, 6000, pricing, false);

        Assert.Equal(PricingAction.DecreasePrice, result.Action);
        Assert.Equal(4999u, result.CalculatedPrice);
        Assert.Equal(PricingReason.UndercutCompetitor, result.Reason);
        Assert.Equal("Competitor", result.ContextData);
    }

    [Fact]
    public void CalculateTargetPrice_WithHqPremium_AppliesPremium() {
        this.configService.GetConfig().Returns(new PluginConfiguration());
        this.ruleProvider.GetRuleForItem(1).Returns(new PricingRule { HqPremiumPercentage = 10.0, UndercutMode = UndercutMode.Absolute, UndercutAmount = 1 });

        var service = new PriceCalculationService(this.configService, this.itemResolver, this.ruleProvider, this.crashProtector);
        var pricing = new MarketItemPricing {
            ItemId = 1,
            Listings = new List<LowestPriceResult> {
                new LowestPriceResult { Price = 1000, RetainerName = "CheapNQ", IsHq = false }
            }
        };

        var result = service.CalculateTargetPrice(1, 5000, pricing, true);

        Assert.Equal(PricingAction.DecreasePrice, result.Action);
        Assert.Equal(1100u, result.CalculatedPrice);
        Assert.Equal(PricingReason.UndercutCompetitor, result.Reason);
    }

    [Fact]
    public void CalculateTargetPrice_WithIgnoreHq_UndercutsLowestRegardlessOfQuality() {
        this.configService.GetConfig().Returns(new PluginConfiguration());
        this.ruleProvider.GetRuleForItem(1).Returns(new PricingRule { IgnoreHq = true, UndercutMode = UndercutMode.Absolute, UndercutAmount = 5 });

        var service = new PriceCalculationService(this.configService, this.itemResolver, this.ruleProvider, this.crashProtector);
        var pricing = new MarketItemPricing {
            ItemId = 1,
            Listings = new List<LowestPriceResult> {
                new LowestPriceResult { Price = 2000, RetainerName = "CheapNQ", IsHq = false },
                new LowestPriceResult { Price = 4000, RetainerName = "ExpensiveHQ", IsHq = true }
            }
        };

        var result = service.CalculateTargetPrice(1, 5000, pricing, true);

        Assert.Equal(PricingAction.DecreasePrice, result.Action);
        Assert.Equal(1995u, result.CalculatedPrice);
        Assert.Equal(PricingReason.UndercutCompetitor, result.Reason);
    }

    [Fact]
    public void CalculateTargetPrice_WithCrashProtection_HoldsPrice() {
        this.configService.GetConfig().Returns(new PluginConfiguration());
        this.ruleProvider.GetRuleForItem(1).Returns(new PricingRule { UndercutMode = UndercutMode.Absolute, UndercutAmount = 1 });

        this.crashProtector.When(x => x.ApplyProtection(Arg.Any<MarketItemPricing>(), Arg.Any<bool>(), Arg.Any<List<LowestPriceResult>>(), out Arg.Any<bool>()))
            .Do(x => x[3] = true);

        var service = new PriceCalculationService(this.configService, this.itemResolver, this.ruleProvider, this.crashProtector);
        var pricing = new MarketItemPricing {
            ItemId = 1,
            Listings = new List<LowestPriceResult> {
                new LowestPriceResult { Price = 500, RetainerName = "Dumper", IsHq = false }
            }
        };

        var result = service.CalculateTargetPrice(1, 6000, pricing, false);

        Assert.Equal(PricingAction.KeepPrice, result.Action);
        Assert.Equal(6000u, result.CalculatedPrice);
        Assert.Equal(PricingReason.MarketCrashProtection, result.Reason);
    }
}