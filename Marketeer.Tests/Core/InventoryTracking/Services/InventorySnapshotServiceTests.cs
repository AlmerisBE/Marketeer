using Dalamud.Plugin.Services;
using Marketeer.API.Configuration.Contracts;
using Marketeer.API.Configuration.Models;
using Marketeer.API.GameInterop.Contracts;
using Marketeer.API.InventoryTracking.Models;
using Marketeer.Core.InventoryTracking.Services;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Core.InventoryTracking.Services;

public class InventorySnapshotServiceTests {

    [Fact]
    public void SaveAndGetSnapshot_UsesConfigurationServiceCorrectly() {
        var mockInventory = Substitute.For<IInventoryService>();
        var mockObjectTable = Substitute.For<IObjectTable>();
        var mockConfig = Substitute.For<IConfigurationService>();

        var pluginConfig = new PluginConfiguration();
        mockConfig.GetConfig().Returns(pluginConfig);

        var service = new InventorySnapshotService(mockInventory, mockObjectTable, mockConfig);

        var snapshot = new InventorySnapshot {
            CharacterName = "Test Player",
            HomeWorldId = 99
        };

        // Act
        service.SaveSnapshot(snapshot);
        var retrieved = service.GetLatestSnapshot("Test Player", 99);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal("Test Player", retrieved.CharacterName);
        mockConfig.Received(1).Save();
    }

    [Fact]
    public void SaveRetainerSnapshot_UsesConfigurationServiceCorrectly() {
        var mockInventory = Substitute.For<IInventoryService>();
        var mockObjectTable = Substitute.For<IObjectTable>();
        var mockConfig = Substitute.For<IConfigurationService>();

        var pluginConfig = new PluginConfiguration();
        mockConfig.GetConfig().Returns(pluginConfig);

        var service = new InventorySnapshotService(mockInventory, mockObjectTable, mockConfig);

        var snapshot = new InventorySnapshot {
            CharacterName = "MyRetainer",
        };

        // Act
        service.SaveRetainerSnapshot(12345ul, snapshot);
        var retrieved = service.GetLatestRetainerSnapshot(12345ul);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal("MyRetainer", retrieved.CharacterName);
        mockConfig.Received(1).Save();
    }
}