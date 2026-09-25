using Marketeer.API.InventoryTracking.Models;
using Marketeer.API.InventoryTracking.Services;
using Marketeer.Core.Configuration.Contracts;
using Marketeer.Core.Configuration.Models;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.API.InventoryTracking.Services;

public class CraftingInventoryServiceTests {
    [Fact]
    public void GetTotalOwnedQuantity_AggregatesHqAndNqItemsAcrossAllSnapshots() {
        var configService = Substitute.For<IConfigurationService>();
        var config = new PluginConfiguration();

        // Simulate Player Inventory
        config.InventorySnapshots["Player_73"] = new InventorySnapshot {
            Items = new List<TrackedItem> {
                new TrackedItem { ItemId = 123, Quantity = 5 }, // NQ Item
                new TrackedItem { ItemId = 1000123, Quantity = 2 } // HQ Item
            }
        };

        // Simulate Retainer Inventory
        config.RetainerInventorySnapshots[1] = new InventorySnapshot {
            Items = new List<TrackedItem> {
                new TrackedItem { ItemId = 123, Quantity = 10 },
                new TrackedItem { ItemId = 999, Quantity = 50 } // Unrelated item
            }
        };

        configService.GetConfig().Returns(config);

        var service = new CraftingInventoryService(configService);

        // Act
        var result = service.GetTotalOwnedQuantity(123);

        // Assert
        Assert.Equal(17u, result); // 5 + 2 + 10 = 17
    }
}