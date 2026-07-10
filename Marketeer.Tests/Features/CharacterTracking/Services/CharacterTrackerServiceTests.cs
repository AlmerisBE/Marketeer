using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin.Services;
using Marketeer.Features.CharacterTracking.Models;
using Marketeer.Features.CharacterTracking.Services;
using Marketeer.Features.Configuration.Contracts;
using Marketeer.Features.Configuration.Models;
using Marketeer.Features.Logging.Contracts;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Features.CharacterTracking.Services;

public class CharacterTrackerServiceTests {
    [Fact]
    public void RecordCurrentCharacter_WhenPlayerIsLoggedIn_AddsCharacterAndSavesConfig() {
        // Arrange
        var mockClientState = Substitute.For<IClientState>();
        var mockObjectTable = Substitute.For<IObjectTable>();
        var mockConfigService = Substitute.For<IConfigurationService>();
        var mockLogger = Substitute.For<ILoggerService>();

        var pluginConfig = new PluginConfiguration();
        mockConfigService.GetConfig().Returns(pluginConfig);

        var mockPlayer = Substitute.For<IPlayerCharacter>();
        mockPlayer.Name.Returns(new Dalamud.Game.Text.SeStringHandling.SeString(
            new List<Dalamud.Game.Text.SeStringHandling.Payload> {
                new Dalamud.Game.Text.SeStringHandling.Payloads.TextPayload("Almeris Tester")
            }));
        mockPlayer.HomeWorld.Returns(_ => default);

        mockObjectTable.LocalPlayer.Returns(mockPlayer);

        var service = new CharacterTrackerService(mockClientState, mockObjectTable, mockConfigService, mockLogger);

        // Act
        service.RecordCurrentCharacter();

        // Assert
        Assert.Single(pluginConfig.KnownCharacters);
        Assert.Equal("Almeris Tester", pluginConfig.KnownCharacters[0].Name);
        Assert.Equal((uint)0, pluginConfig.KnownCharacters[0].HomeWorldId);
        mockConfigService.Received(1).Save();
    }

    [Fact]
    public void RecordCurrentCharacter_WhenCharacterAlreadyKnown_DoesNotDuplicateOrSave() {
        // Arrange
        var mockClientState = Substitute.For<IClientState>();
        var mockObjectTable = Substitute.For<IObjectTable>();
        var mockConfigService = Substitute.For<IConfigurationService>();
        var mockLogger = Substitute.For<ILoggerService>();

        var pluginConfig = new PluginConfiguration {
            KnownCharacters = new List<TrackedCharacter> {
                new TrackedCharacter { Name = "Almeris Tester", HomeWorldId = 0 }
            }
        };
        mockConfigService.GetConfig().Returns(pluginConfig);

        var mockPlayer = Substitute.For<IPlayerCharacter>();
        mockPlayer.Name.Returns(new Dalamud.Game.Text.SeStringHandling.SeString(
            new List<Dalamud.Game.Text.SeStringHandling.Payload> {
                new Dalamud.Game.Text.SeStringHandling.Payloads.TextPayload("Almeris Tester")
            }));
        mockPlayer.HomeWorld.Returns(_ => default);

        mockObjectTable.LocalPlayer.Returns(mockPlayer);

        var service = new CharacterTrackerService(mockClientState, mockObjectTable, mockConfigService, mockLogger);

        // Act
        service.RecordCurrentCharacter();

        // Assert
        Assert.Single(pluginConfig.KnownCharacters);
        mockConfigService.DidNotReceive().Save();
    }
}