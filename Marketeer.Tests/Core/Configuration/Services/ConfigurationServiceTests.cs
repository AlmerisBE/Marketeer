using Dalamud.Configuration;
using Dalamud.Plugin;
using Marketeer.Core.Configuration.Models;
using Marketeer.Core.Configuration.Services;
using Marketeer.Core.Financials.Models;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Core.Configuration.Services;

public class ConfigurationServiceTests {
    [Fact]
    public void ConfigurationService_Initialization_LoadsExistingConfig() {
        // Arrange
        var mockPluginInterface = Substitute.For<IDalamudPluginInterface>();
        var existingConfig = new PluginConfiguration {
            Version = 1
        };
        existingConfig.FinancialRecords.Add("Test_0", new CharacterFinancialData { CharacterName = "Test" });

        mockPluginInterface.GetPluginConfig().Returns(existingConfig);

        // Act
        var service = new ConfigurationService(mockPluginInterface);
        var config = service.GetConfig();

        // Assert
        Assert.NotNull(config);
        Assert.Equal(1, config.Version);
        Assert.Single(config.FinancialRecords);
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
        Assert.Equal(0, config.Version);
        Assert.NotNull(config.FinancialRecords);
        Assert.Empty(config.FinancialRecords);
    }

    [Fact]
    public void ConfigurationService_Save_PassesConfigToDalamud() {
        // Arrange
        var mockPluginInterface = Substitute.For<IDalamudPluginInterface>();
        var service = new ConfigurationService(mockPluginInterface);
        var config = service.GetConfig();

        config.Version = 2;

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