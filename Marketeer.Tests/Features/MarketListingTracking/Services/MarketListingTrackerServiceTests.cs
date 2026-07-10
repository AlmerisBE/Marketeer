using Marketeer.Features.Configuration.Contracts;
using Marketeer.Features.Configuration.Models;
using Marketeer.Features.GameEvents.Contracts;
using Marketeer.Features.Logging.Contracts;
using Marketeer.Features.MarketListingTracking.Contracts;
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
        var mockGameEventService = Substitute.For<IGameEventService>();
        var mockProvider = Substitute.For<IMarketListingProvider>();

        var pluginConfig = new PluginConfiguration {
            KnownListings = new List<TrackedListing> {
                new TrackedListing { AssociatedRetainerId = 100, SlotIndex = 0, ItemId = 5000, Quantity = 1, PricePerUnit = 100 },
                new TrackedListing { AssociatedRetainerId = 200, SlotIndex = 0, ItemId = 9999, Quantity = 99, PricePerUnit = 300 }
            }
        };

        mockConfigService.GetConfig().Returns(pluginConfig);

        var service = new MarketListingTrackerService(mockConfigService, mockGameEventService, mockProvider, mockLogger);

        // Act
        var resultForRetainer100 = service.GetListingsForRetainer(100);

        // Assert
        Assert.Single(resultForRetainer100);
        Assert.Equal(5000u, resultForRetainer100[0].ItemId);
    }

    [Fact]
    public void RecordListings_WhenEventFires_ReplacesOldListingsBasedOnSlotIndexAndSaves() {
        // Arrange
        var mockConfigService = Substitute.For<IConfigurationService>();
        var mockLogger = Substitute.For<ILoggerService>();
        var mockGameEventService = Substitute.For<IGameEventService>();
        var mockProvider = Substitute.For<IMarketListingProvider>();

        var pluginConfig = new PluginConfiguration {
            KnownListings = new List<TrackedListing> {
                new TrackedListing { AssociatedRetainerId = 100, SlotIndex = 0, ItemId = 1111, Quantity = 1, PricePerUnit = 100 },
                new TrackedListing { AssociatedRetainerId = 200, SlotIndex = 0, ItemId = 2222, Quantity = 1, PricePerUnit = 200 }
            }
        };
        mockConfigService.GetConfig().Returns(pluginConfig);

        mockProvider.GetActiveRetainerId().Returns(100ul);
        mockProvider.GetActiveRetainerListings().Returns(new List<TrackedListing> {
            new TrackedListing { AssociatedRetainerId = 100, SlotIndex = 0, ItemId = 9999, Quantity = 5, PricePerUnit = 500 },
            new TrackedListing { AssociatedRetainerId = 100, SlotIndex = 1, ItemId = 9999, Quantity = 3, PricePerUnit = 500 }
        });

        var service = new MarketListingTrackerService(mockConfigService, mockGameEventService, mockProvider, mockLogger);

        // Act
        mockGameEventService.RetainerListingsOpened += Raise.Event<Action>();

        // Assert
        Assert.Equal(3, pluginConfig.KnownListings.Count);
        Assert.Contains(pluginConfig.KnownListings, l => l.ItemId == 9999 && l.AssociatedRetainerId == 100 && l.SlotIndex == 0);
        Assert.Contains(pluginConfig.KnownListings, l => l.ItemId == 9999 && l.AssociatedRetainerId == 100 && l.SlotIndex == 1);
        Assert.DoesNotContain(pluginConfig.KnownListings, l => l.ItemId == 1111);

        mockConfigService.Received(1).Save();
    }
}