using Marketeer.Core.Configuration.Contracts;
using Marketeer.Core.Configuration.Models;
using Marketeer.Core.Financials.Models;
using Marketeer.Core.MarketListings.Services;
using Marketeer.Core.SalesHistory.Contracts;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Core.MarketListings.Services;

public class ListingOptimizationServiceTests {
    [Fact]
    public void GetVendorPricedListings_WhenItemsPricedBelowVendor_ReturnsListings() {
        // Arrange
        var mockConfigService = Substitute.For<IConfigurationService>();
        var mockItemResolver = Substitute.For<IItemResolverService>();

        var config = new PluginConfiguration();
        var charData = new CharacterFinancialData { CharacterName = "Tester" };
        var retainer = new RetainerFinancialData { Name = "Seller" };

        // Item 100 is priced at 5 (Vendor price is 10) -> Should trigger
        retainer.MarketListings.Add(0, new RetainerMarketListingSaveData { ItemId = 100, PricePerUnit = 5 });
        // Item 200 is priced at 50 (Vendor price is 10) -> Should NOT trigger
        retainer.MarketListings.Add(1, new RetainerMarketListingSaveData { ItemId = 200, PricePerUnit = 50 });

        charData.Retainers.Add(1, retainer);
        config.FinancialRecords.Add("Tester_0", charData);

        mockConfigService.GetConfig().Returns(config);
        mockItemResolver.ResolveItemName(100).Returns("Cheap Potion");

        // Mocking behavior through our own service, completely avoiding Lumina struct restrictions
        mockItemResolver.ResolveVendorPrice(100).Returns(10u);
        mockItemResolver.ResolveVendorPrice(200).Returns(10u);

        var service = new ListingOptimizationService(mockConfigService, mockItemResolver);

        // Act
        var results = service.GetVendorPricedListings();

        // Assert
        Assert.Single(results);
        Assert.Equal(100u, results[0].ItemId);
        Assert.Equal("Cheap Potion", results[0].ItemName);
        Assert.Equal(5u, results[0].Price);
        Assert.Equal(10u, results[0].VendorPrice);
    }
}