using BasePlugin.Features.Configuration.Models;
using BasePlugin.Features.Configuration.Services;
using Dalamud.Configuration;
using Dalamud.Plugin;
using NSubstitute;
using Xunit;

namespace BasePlugin.Tests.Features.Configuration.Services;

public class ConfigurationServiceTests {
    [Fact]
    public void ConfigurationService_Initialization_LoadsExistingConfig() {
        // Arrange
        var mockPluginInterface = Substitute.For<IDalamudPluginInterface>();
        var existingConfig = new PluginConfiguration {
            Version = 1,
            ExampleCheckbox = true
        };

        // Mock Dalamud returning an existing configuration file
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

        // Mock Dalamud returning null (first time the plugin is launched)
        mockPluginInterface.GetPluginConfig().Returns((IPluginConfiguration)null!);

        // Act
        var service = new ConfigurationService(mockPluginInterface);
        var config = service.GetConfig();

        // Assert
        Assert.NotNull(config);
        Assert.False(config.ExampleCheckbox); // Default value expected
        Assert.Equal(0, config.Version);
    }

    [Fact]
    public void ConfigurationService_Save_PassesConfigToDalamud() {
        // Arrange
        var mockPluginInterface = Substitute.For<IDalamudPluginInterface>();
        var service = new ConfigurationService(mockPluginInterface);
        var config = service.GetConfig();

        // Modify a value to simulate user interaction in the UI
        config.ExampleCheckbox = true;

        // Act
        service.Save();

        // Assert
        // Verify that SavePluginConfig was called exactly once with our config object
        mockPluginInterface.Received(1).SavePluginConfig(config);
    }
}