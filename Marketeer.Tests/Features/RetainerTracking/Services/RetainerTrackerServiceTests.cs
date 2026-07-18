using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin.Services;
using Marketeer.Features.CharacterTracking.Contracts;
using Marketeer.Features.Configuration.Contracts;
using Marketeer.Features.Configuration.Models;
using Marketeer.Features.Financials.Models;
using Marketeer.Features.GameEvents.Contracts;
using Marketeer.Features.Logging.Contracts;
using Marketeer.Features.RetainerTracking.Contracts;
using Marketeer.Features.RetainerTracking.Models;
using Marketeer.Features.RetainerTracking.Services;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Features.RetainerTracking.Services;

public class RetainerTrackerServiceTests {
    [Fact]
    public void OnRetainerBellOpened_WhenActiveCharacterExists_StoresRetainersInFinancials() {
        // Arrange
        var mockObjectTable = Substitute.For<IObjectTable>();
        var mockConfigService = Substitute.For<IConfigurationService>();
        var mockRetainerProvider = Substitute.For<IRetainerProvider>();
        var mockCharacterTracker = Substitute.For<ICharacterTrackerService>();
        var mockGameEventService = Substitute.For<IGameEventService>();
        var mockLogger = Substitute.For<ILoggerService>();

        var mockPlayer = Substitute.For<IPlayerCharacter>();
        mockPlayer.Name.Returns(new Dalamud.Game.Text.SeStringHandling.SeString(
            new List<Dalamud.Game.Text.SeStringHandling.Payload> {
                new Dalamud.Game.Text.SeStringHandling.Payloads.TextPayload("Active Player")
            }));
        mockPlayer.HomeWorld.Returns(_ => default);

        mockObjectTable.LocalPlayer.Returns(mockPlayer);

        var pluginConfig = new PluginConfiguration();
        pluginConfig.FinancialRecords.Add("Active Player_0", new CharacterFinancialData { CharacterName = "Active Player", HomeWorldId = 0 });
        mockConfigService.GetConfig().Returns(pluginConfig);

        var gameRetainers = new List<TrackedRetainer> {
            new TrackedRetainer { Name = "FirstRetainer", RetainerId = 100 }
        };
        mockRetainerProvider.GetActiveRetainers().Returns(gameRetainers);

        var service = new RetainerTrackerService(mockObjectTable, mockConfigService, mockRetainerProvider, mockCharacterTracker, mockGameEventService, mockLogger);

        // Act
        mockGameEventService.RetainerBellOpened += Raise.Event<Action>();

        // Assert
        Assert.Single(pluginConfig.FinancialRecords["Active Player_0"].Retainers);
        Assert.Equal("FirstRetainer", pluginConfig.FinancialRecords["Active Player_0"].Retainers[100].Name);
        mockConfigService.Received(1).Save();
    }
}