using Marketeer.Features.Configuration.Contracts;
using Marketeer.Features.Configuration.Models;
using Marketeer.Features.Logging.Contracts;
using Marketeer.Features.MarketListingTracking.Models;
using Marketeer.Features.MarketListingTracking.Services;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Features.MarketListingTracking.Services;

public class MarketListingTrackerServiceTests {
    [Fact]
    public void GetListingsForRetainer_ReturnsMappedDisplayDataForAssociatedRetainer() {
        // Arrange
        var mockConfigService = Substitute.For<IConfigurationService>();
        var mockLogger = Substitute.For<ILoggerService>();

        var pluginConfig = new PluginConfiguration {
            KnownListings = new List<TrackedListing> {
                new TrackedListing { AssociatedRetainerId = 100, ItemId = 5000, Quantity = 1, PricePerUnit = 100 },
                new TrackedListing { AssociatedRetainerId = 100, ItemId = 5001, Quantity = 5, PricePerUnit = 200 },
                new TrackedListing { AssociatedRetainerId = 200, ItemId = 9999, Quantity = 99, PricePerUnit = 300 }
            }
        };

        mockConfigService.GetConfig().Returns(pluginConfig);

        var service = new MarketListingTrackerService(mockConfigService, mockLogger);

        // Act
        var resultForRetainer100 = service.GetListingsForRetainer(100);
        var resultForRetainer200 = service.GetListingsForRetainer(200);
        var resultForUnknown = service.GetListingsForRetainer(999);

        // Assert
        Assert.Equal(2, resultForRetainer100.Count);
        Assert.Equal(5000u, resultForRetainer100[0].ItemId);
        Assert.Equal(100u, resultForRetainer100[0].PricePerUnit);

        Assert.Single(resultForRetainer200);
        Assert.Equal(9999u, resultForRetainer200[0].ItemId);

        Assert.Empty(resultForUnknown);
    }
}