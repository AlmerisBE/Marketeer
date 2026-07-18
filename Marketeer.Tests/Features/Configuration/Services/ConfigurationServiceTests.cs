using Dalamud.Configuration;
using Dalamud.Plugin;
using Marketeer.Features.Configuration.Models;
using Marketeer.Features.Configuration.Services;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Features.Configuration.Services;

public class ConfigurationServiceTests {
    [Fact]
    public void ConfigurationService_Initialization_LoadsExistingConfig() {
        // Arrange
        var mockPluginInterface = Substitute.For<IDalamudPluginInterface>();
        var existingConfig = new PluginConfiguration {
            Version = 1,
            ExampleCheckbox = true
        };

        mockPluginInterface.GetPluginConfig().Returns(existingConfig);

        // Act
        var service = new ConfigurationService(mockPluginInterface);
        var config = service.GetConfig();

        // Assert
        Assert.NotNull(config);
        Assert.True(config.ExampleCheckbox);
        Assert.Equal(1, config.Version);
    }

    [Fact]
    public void ConfigurationService_Initialization_CreatesNewConfigIfNull() {
        // Arrange
        var mockPluginInterface = Substitute.For<IDalamudPluginInterface>();
        mockPluginInterface.GetPluginConfig().Returns((IPluginConfiguration)null!);

        // Act
        var service = new ConfigurationService(mockPluginInterface);
        var config = service.GetConfig();

        // Assert
        Assert.NotNull(config);
        Assert.False(config.ExampleCheckbox);
        Assert.Equal(0, config.Version);
    }

    [Fact]
    public void ConfigurationService_Save_PassesConfigToDalamud() {
        // Arrange
        var mockPluginInterface = Substitute.For<IDalamudPluginInterface>();
        var service = new ConfigurationService(mockPluginInterface);
        var config = service.GetConfig();

        config.ExampleCheckbox = true;

        // Act
        service.Save();

        // Assert
        mockPluginInterface.Received(1).SavePluginConfig(config);
    }

    [Fact]
    public void ConfigurationService_Initialization_EnsuresFinancialRecordsCollectionIsNotNull() {
        // Arrange
        var mockPluginInterface = Substitute.For<IDalamudPluginInterface>();
        var oldConfig = new PluginConfiguration {
            Version = 1,
            FinancialRecords = null!
        };
        mockPluginInterface.GetPluginConfig().Returns(oldConfig);

        // Act
        var service = new ConfigurationService(mockPluginInterface);
        var config = service.GetConfig();

        // Assert
        Assert.NotNull(config.FinancialRecords);
    }
}