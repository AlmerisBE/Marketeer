using Dalamud.Plugin.Services;
using Marketeer.Core.Configuration.Contracts;
using Marketeer.Core.Configuration.Models;
using Marketeer.Core.MarketStrategy.Models;
using Marketeer.Core.MarketStrategy.Services;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Core.MarketStrategy.Services;

public class PricingRuleResolverTests {

    // Helper class using the "Extract and Override" pattern to bypass strict Lumina ExcelSheet struct initializations
    private class TestablePricingRuleResolver : PricingRuleResolver {
        public uint MockedCategoryId { get; set; } = 0;

        public TestablePricingRuleResolver(IConfigurationService configService, IDataManager dataManager)
            : base(configService, dataManager) { }

        protected override uint GetItemCategory(uint baseItemId) {
            return this.MockedCategoryId;
        }
    }

    [Fact]
    public void GetRuleForItem_WithNoMappings_ReturnsGlobalConfig() {
        var configService = Substitute.For<IConfigurationService>();
        var dataManager = Substitute.For<IDataManager>();

        var config = new PluginConfiguration {
            UndercutMode = UndercutMode.Absolute,
            UndercutAmount = 5,
            GlobalIgnoreHq = false,
            GlobalHqPremiumPercentage = 10.0
        };
        configService.GetConfig().Returns(config);

        var resolver = new TestablePricingRuleResolver(configService, dataManager);

        var result = resolver.GetRuleForItem(1234);

        Assert.Equal(UndercutMode.Absolute, result.UndercutMode);
        Assert.Equal(5u, result.UndercutAmount);
        Assert.False(result.IgnoreHq);
        Assert.Equal(10.0, result.HqPremiumPercentage);
    }

    [Fact]
    public void GetRuleForItem_WithItemMapping_OverridesGlobalConfig() {
        var configService = Substitute.For<IConfigurationService>();
        var dataManager = Substitute.For<IDataManager>();

        var config = new PluginConfiguration {
            UndercutAmount = 5,
            GlobalIgnoreHq = false
        };

        var profile = new PricingProfile { Id = "prof1", Rule = new PricingRule { IgnoreHq = true, UndercutAmount = 10 } };
        config.PricingProfiles["prof1"] = profile;
        config.ItemProfileMappings[1234] = "prof1";

        configService.GetConfig().Returns(config);

        var resolver = new TestablePricingRuleResolver(configService, dataManager);

        var result = resolver.GetRuleForItem(1234);

        Assert.True(result.IgnoreHq);
        Assert.Equal(10u, result.UndercutAmount);
        Assert.Equal(config.UndercutRelativePercentage, result.UndercutRelativePercentage);
    }

    [Fact]
    public void GetRuleForItem_WithItemAndCategoryMapping_ItemTakesPrecedence() {
        var configService = Substitute.For<IConfigurationService>();
        var dataManager = Substitute.For<IDataManager>();

        var config = new PluginConfiguration { UndercutAmount = 5 };

        var catProfile = new PricingProfile { Id = "catProf", Rule = new PricingRule { UndercutAmount = 20, HqPremiumPercentage = 15.0 } };
        var itemProfile = new PricingProfile { Id = "itemProf", Rule = new PricingRule { UndercutAmount = 50 } };

        config.PricingProfiles["catProf"] = catProfile;
        config.PricingProfiles["itemProf"] = itemProfile;
        config.CategoryProfileMappings[99] = "catProf";
        config.ItemProfileMappings[1234] = "itemProf";

        configService.GetConfig().Returns(config);

        // Inject the mocked category ID through the testable subclass
        var resolver = new TestablePricingRuleResolver(configService, dataManager) {
            MockedCategoryId = 99
        };

        var result = resolver.GetRuleForItem(1234);

        Assert.Equal(50u, result.UndercutAmount);
        Assert.Equal(15.0, result.HqPremiumPercentage);
        Assert.Equal(config.UndercutRelativePercentage, result.UndercutRelativePercentage);
    }
}