using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin.Services;
using Marketeer.Features.CharacterTracking.Contracts;
using Marketeer.Features.Configuration.Contracts;
using Marketeer.Features.Configuration.Models;
using Marketeer.Features.Logging.Contracts;
using Marketeer.Features.RetainerTracking.Contracts;
using Marketeer.Features.RetainerTracking.Models;
using Marketeer.Features.RetainerTracking.Services;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Features.RetainerTracking.Services;

public class RetainerTrackerServiceTests {
    [Fact]
    public void RecordRetainers_WhenActiveCharacterExists_StoresRetainersWithForeignKeys() {
        // Arrange
        var mockObjectTable = Substitute.For<IObjectTable>();
        var mockConfigService = Substitute.For<IConfigurationService>();
        var mockRetainerProvider = Substitute.For<IRetainerProvider>();
        var mockCharacterTracker = Substitute.For<ICharacterTrackerService>();
        var mockLogger = Substitute.For<ILoggerService>();

        var mockPlayer = Substitute.For<IPlayerCharacter>();
        mockPlayer.Name.Returns(new Dalamud.Game.Text.SeStringHandling.SeString(
            new List<Dalamud.Game.Text.SeStringHandling.Payload> {
                new Dalamud.Game.Text.SeStringHandling.Payloads.TextPayload("Active Player")
            }));
        mockPlayer.HomeWorld.Returns(_ => default); // ID 0
        mockObjectTable.LocalPlayer.Returns(mockPlayer);

        var pluginConfig = new PluginConfiguration();
        mockConfigService.GetConfig().Returns(pluginConfig);

        var gameRetainers = new List<TrackedRetainer> {
            new TrackedRetainer { Name = "FirstRetainer", RetainerId = 100 }
        };
        mockRetainerProvider.GetActiveRetainers().Returns(gameRetainers);

        var service = new RetainerTrackerService(mockObjectTable, mockConfigService, mockRetainerProvider, mockCharacterTracker, mockLogger);

        // Act
        service.RecordRetainers();

        // Assert
        Assert.Single(pluginConfig.KnownRetainers);
        Assert.Equal("Active Player", pluginConfig.KnownRetainers[0].AssociatedCharacterName);
        Assert.Equal(0u, pluginConfig.KnownRetainers[0].AssociatedHomeWorldId); // Also ensured '0u' here for consistency
        mockConfigService.Received(1).Save();
    }

    [Fact]
    public void OnCharacterForgotten_RemovesAssociatedRetainersAndSaves() {
        // Arrange
        var mockObjectTable = Substitute.For<IObjectTable>();
        var mockConfigService = Substitute.For<IConfigurationService>();
        var mockRetainerProvider = Substitute.For<IRetainerProvider>();
        var mockCharacterTracker = Substitute.For<ICharacterTrackerService>();
        var mockLogger = Substitute.For<ILoggerService>();

        mockObjectTable.LocalPlayer.Returns((IPlayerCharacter?)null);

        var pluginConfig = new PluginConfiguration {
            KnownRetainers = new List<TrackedRetainer> {
                new TrackedRetainer { Name = "Retainer A", AssociatedCharacterName = "Deleted Player", AssociatedHomeWorldId = 99 },
                new TrackedRetainer { Name = "Retainer B", AssociatedCharacterName = "Other Player", AssociatedHomeWorldId = 33 }
            }
        };
        mockConfigService.GetConfig().Returns(pluginConfig);

        var service = new RetainerTrackerService(mockObjectTable, mockConfigService, mockRetainerProvider, mockCharacterTracker, mockLogger);

        // Act
        // Added the 'u' suffix to 99 to correctly match the Action<string, uint> signature
        mockCharacterTracker.CharacterForgotten += Raise.Event<Action<string, uint>>("Deleted Player", 99u);

        // Assert
        Assert.Single(pluginConfig.KnownRetainers);
        Assert.Equal("Retainer B", pluginConfig.KnownRetainers[0].Name);
        mockConfigService.Received(1).Save();
    }
}