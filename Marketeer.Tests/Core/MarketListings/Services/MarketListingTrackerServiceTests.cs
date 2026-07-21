using Dalamud.Plugin.Services;
using Marketeer.API.Configuration.Contracts;
using Marketeer.API.Configuration.Models;
using Marketeer.API.Financials.Models;
using Marketeer.API.GameInterop.Contracts;
using Marketeer.API.Logging.Contracts;
using Marketeer.API.MarketListings.Models;
using Marketeer.API.SalesHistory.Contracts;
using Marketeer.API.SalesHistory.Models;
using Marketeer.Core.MarketListings.Services;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Core.MarketListings.Services;

public class MarketListingTrackerServiceTests {

    [Fact]
    public void GetListingsForRetainer_ReturnsMappedDisplayDataFromFinancialRecords() {
        // Arrange
        var mockConfigService = Substitute.For<IConfigurationService>();
        var mockLogger = Substitute.For<ILoggerService>();
        var mockGameEventService = Substitute.For<IGameEventService>();
        var mockProvider = Substitute.For<IMarketListingProvider>();
        var mockDataManager = Substitute.For<IDataManager>();
        var mockInferenceService = Substitute.For<ISalesInferenceService>();

        var pluginConfig = new PluginConfiguration();
        var charData = new CharacterFinancialData { CharacterName = "Almeris", HomeWorldId = 33 };
        var retData = new RetainerFinancialData { RetainerId = 100 };

        retData.MarketListings.Add(0, new RetainerMarketListingSaveData { ItemId = 5000, Quantity = 1, PricePerUnit = 100 });
        charData.Retainers.Add(100, retData);
        pluginConfig.FinancialRecords.Add("Almeris_33", charData);

        mockConfigService.GetConfig().Returns(pluginConfig);

        var service = new MarketListingTrackerService(mockConfigService, mockGameEventService, mockProvider, mockDataManager, mockInferenceService, mockLogger);

        // Act
        var result = service.GetListingsForRetainer(100);

        // Assert
        Assert.Single(result);
        Assert.Equal(5000u, result[0].ItemId);
        Assert.Equal("Unknown Item", result[0].ItemName);
    }

    [Fact]
    public void ScanListings_WhenInvoked_ExecutesInferenceAndSavesConfiguration() {
        // Arrange
        var mockConfigService = Substitute.For<IConfigurationService>();
        var mockLogger = Substitute.For<ILoggerService>();
        var mockGameEventService = Substitute.For<IGameEventService>();
        var mockProvider = Substitute.For<IMarketListingProvider>();
        var mockDataManager = Substitute.For<IDataManager>();
        var mockInferenceService = Substitute.For<ISalesInferenceService>();

        var pluginConfig = new PluginConfiguration();
        var charData = new CharacterFinancialData { CharacterName = "Tester", HomeWorldId = 1 };
        var retData = new RetainerFinancialData { RetainerId = 200 };

        charData.Retainers.Add(200, retData);
        pluginConfig.FinancialRecords.Add("Tester_1", charData);
        mockConfigService.GetConfig().Returns(pluginConfig);

        mockProvider.GetActiveRetainerId().Returns(200ul);
        mockProvider.GetActiveRetainerListings().Returns(new List<TrackedListing> {
            new TrackedListing { SlotIndex = 1, ItemId = 999, Quantity = 5, PricePerUnit = 1000 }
        });

        var service = new MarketListingTrackerService(mockConfigService, mockGameEventService, mockProvider, mockDataManager, mockInferenceService, mockLogger);

        // Act
        var success = service.ScanListings(200ul, true);

        // Assert
        Assert.True(success);

        mockInferenceService.Received(1).InferSales(
            200ul,
            true,
            Arg.Any<IReadOnlyList<ListingState>>(),
            Arg.Any<IReadOnlyList<ListingState>>());

        mockConfigService.Received(1).Save();
        Assert.Single(retData.MarketListings);
        Assert.Equal(999u, retData.MarketListings[1].ItemId);
    }
}