using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin.Services;
using Marketeer.API.GameInterop.Contracts;
using Marketeer.Core.CharacterManagement.Contracts;
using Marketeer.Core.CharacterManagement.Models;
using Marketeer.Core.CharacterManagement.Services;
using Marketeer.Core.Configuration.Contracts;
using Marketeer.Core.Configuration.Models;
using Marketeer.Core.Financials.Models;
using Marketeer.Core.Logging.Contracts;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Core.CharacterManagement.Services;

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
        var mockInventoryService = Substitute.For<IInventoryService>();

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

        var service = new RetainerTrackerService(mockObjectTable, mockConfigService, mockRetainerProvider, mockCharacterTracker, mockGameEventService, mockLogger, mockInventoryService);

        // Act
        mockGameEventService.RetainerBellOpened += Raise.Event<Action>();

        // Assert
        Assert.Single(pluginConfig.FinancialRecords["Active Player_0"].Retainers);
        Assert.Equal("FirstRetainer", pluginConfig.FinancialRecords["Active Player_0"].Retainers[100].Name);
        mockConfigService.Received(1).Save();
    }
}