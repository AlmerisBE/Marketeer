using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin.Services;
using Marketeer.API.GameInterop.Contracts;
using Marketeer.Core.CharacterManagement.Services;
using Marketeer.Core.Configuration.Contracts;
using Marketeer.Core.Configuration.Models;
using Marketeer.Core.Logging.Contracts;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Core.CharacterManagement.Services;

public class CharacterTrackerServiceTests {
    [Fact]
    public void RecordCurrentCharacter_WhenPlayerIsLoggedIn_AddsCharacterAndSavesConfig() {
        // Arrange
        var mockClientState = Substitute.For<IClientState>();
        var mockObjectTable = Substitute.For<IObjectTable>();
        var mockConfigService = Substitute.For<IConfigurationService>();
        var mockLogger = Substitute.For<ILoggerService>();
        var mockFramework = Substitute.For<IFramework>();
        var mockInventoryService = Substitute.For<IInventoryService>();

        mockFramework.When(x => x.RunOnFrameworkThread(Arg.Any<Action>())).Do(cb => cb.Arg<Action>()());
        mockClientState.IsLoggedIn.Returns(true);

        var pluginConfig = new PluginConfiguration();
        mockConfigService.GetConfig().Returns(pluginConfig);

        var mockPlayer = Substitute.For<IPlayerCharacter>();

        mockPlayer.Name.Returns(new Dalamud.Game.Text.SeStringHandling.SeString(
            new List<Dalamud.Game.Text.SeStringHandling.Payload> {
                new Dalamud.Game.Text.SeStringHandling.Payloads.TextPayload("Almeris Tester")
            }));

        mockPlayer.CompanyTag.Returns(new Dalamud.Game.Text.SeStringHandling.SeString(
            new List<Dalamud.Game.Text.SeStringHandling.Payload> {
                new Dalamud.Game.Text.SeStringHandling.Payloads.TextPayload("FC")
            }));

        mockPlayer.HomeWorld.Returns(_ => default);

        mockObjectTable.LocalPlayer.Returns(mockPlayer);

        // Mock Gil (ItemId 1)
        mockInventoryService.GetItemCountInInventory(1).Returns(15000);

        var service = new CharacterTrackerService(mockClientState, mockObjectTable, mockConfigService, mockLogger, mockFramework, mockInventoryService);

        mockConfigService.ClearReceivedCalls();

        // Act
        service.RecordCurrentCharacter();

        // Assert
        Assert.Single(pluginConfig.FinancialRecords);
        Assert.True(pluginConfig.FinancialRecords.ContainsKey("Almeris Tester_0"));
        Assert.Equal(15000ul, pluginConfig.FinancialRecords["Almeris Tester_0"].CharacterGil);
        mockConfigService.Received(1).Save();
    }
}