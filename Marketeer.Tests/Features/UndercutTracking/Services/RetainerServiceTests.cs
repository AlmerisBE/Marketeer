using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Plugin.Services;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using Marketeer.Features.Configuration.Contracts;
using Marketeer.Features.Configuration.Models;
using Marketeer.Features.Financials.Models;
using Marketeer.Features.UndercutTracking.Models;
using Marketeer.Features.UndercutTracking.Services;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Features.UndercutTracking.Services;

public class RetainerStateServiceTests {

    [Fact]
    public void GetCurrentListings_WhenConfigHasListings_ReturnsMappedRetainerListings() {
        // Arrange
        var mockConfigService = Substitute.For<IConfigurationService>();
        var mockObjectTable = Substitute.For<IObjectTable>();

        var pluginConfig = new PluginConfiguration();
        var charData = new CharacterFinancialData { CharacterName = "Tester", HomeWorldId = 33 };
        var retData = new RetainerFinancialData { RetainerId = 1, Name = "MarketRetainer" };

        retData.MarketListings.Add(0, new RetainerMarketListingSaveData { ItemId = 100, Quantity = 1, PricePerUnit = 5000 });
        charData.Retainers.Add(1, retData);
        pluginConfig.FinancialRecords.Add("Tester_33", charData);

        mockConfigService.GetConfig().Returns(pluginConfig);

        var mockPlayer = Substitute.For<IPlayerCharacter>();
        var playerSeString = new SeString(new List<Payload> { new TextPayload("Tester") });
        mockPlayer.Name.Returns(playerSeString);

        mockPlayer.HomeWorld.Returns(new RowRef<World>(null, 33u));
        mockObjectTable.LocalPlayer.Returns(mockPlayer);

        var service = new RetainerStateService(mockConfigService, mockObjectTable);

        // Act
        var result = service.GetCurrentListings();

        // Assert
        Assert.Single(result);
        Assert.Equal(100u, result[0].ItemId);
        Assert.Equal("MarketRetainer", result[0].RetainerName);
        Assert.Equal(5000u, result[0].CurrentPrice);
    }

    [Fact]
    public void UpdateListings_WithManualListings_OverridesConfiguredListings() {
        // Arrange
        var mockConfigService = Substitute.For<IConfigurationService>();
        var mockObjectTable = Substitute.For<IObjectTable>();

        var service = new RetainerStateService(mockConfigService, mockObjectTable);
        var newListings = new List<RetainerListing> {
            new RetainerListing { ItemId = 42, RetainerName = "Almeris", CurrentPrice = 1000 }
        };

        // Act
        service.UpdateListings(newListings);
        var retrievedItems = service.GetCurrentListings();

        // Assert
        Assert.Single(retrievedItems);
        Assert.Equal("Almeris", retrievedItems[0].RetainerName);
        Assert.Equal(1000u, retrievedItems[0].CurrentPrice);
    }

    [Fact]
    public void UpdateListings_FiresListingsUpdatedEvent() {
        // Arrange
        var mockConfigService = Substitute.For<IConfigurationService>();
        var mockObjectTable = Substitute.For<IObjectTable>();

        var service = new RetainerStateService(mockConfigService, mockObjectTable);
        var newListings = new List<RetainerListing> {
            new RetainerListing { ItemId = 42, RetainerName = "Almeris", CurrentPrice = 1000 }
        };

        var eventFired = false;
        service.ListingsUpdated += listings => {
            eventFired = true;
        };

        // Act
        service.UpdateListings(newListings);

        // Assert
        Assert.True(eventFired);
    }
}