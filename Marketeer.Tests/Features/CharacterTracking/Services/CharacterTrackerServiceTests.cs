using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin.Services;
using Marketeer.Features.CharacterTracking.Services;
using Marketeer.Features.Configuration.Contracts;
using Marketeer.Features.Configuration.Models;
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
        var mockLogger = Substitute.For<Marketeer.Features.Logging.Contracts.ILoggerService>();
        var mockFramework = Substitute.For<IFramework>();

        mockFramework.When(x => x.RunOnFrameworkThread(Arg.Any<Action>())).Do(cb => cb.Arg<Action>()());
        mockClientState.IsLoggedIn.Returns(true);

        var pluginConfig = new PluginConfiguration();
        mockConfigService.GetConfig().Returns(pluginConfig);

        var mockPlayer = Substitute.For<IPlayerCharacter>();
        mockPlayer.Name.Returns(new Dalamud.Game.Text.SeStringHandling.SeString(
            new List<Dalamud.Game.Text.SeStringHandling.Payload> {
                new Dalamud.Game.Text.SeStringHandling.Payloads.TextPayload("Almeris Tester")
            }));
        mockPlayer.HomeWorld.Returns(_ => default);

        mockObjectTable.LocalPlayer.Returns(mockPlayer);

        var service = new CharacterTrackerService(mockClientState, mockObjectTable, mockConfigService, mockLogger, mockFramework);

        // Act
        service.RecordCurrentCharacter();

        // Assert
        Assert.Single(pluginConfig.FinancialRecords);
        Assert.True(pluginConfig.FinancialRecords.ContainsKey("Almeris Tester_0"));
        mockConfigService.Received(1).Save();
    }
}