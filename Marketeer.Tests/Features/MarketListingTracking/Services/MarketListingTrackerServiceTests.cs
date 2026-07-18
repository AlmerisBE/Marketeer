using Dalamud.Plugin.Services;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using Marketeer.Features.Configuration.Contracts;
using Marketeer.Features.Configuration.Models;
using Marketeer.Features.Financials.Models;
using Marketeer.Features.GameEvents.Contracts;
using Marketeer.Features.Logging.Contracts;
using Marketeer.Features.MarketListingTracking.Contracts;
using Marketeer.Features.MarketListingTracking.Services;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Features.MarketListingTracking.Services;

public class MarketListingTrackerServiceTests {
    [Fact]
    public void GetListingsForRetainer_ReturnsMappedDisplayDataFromFinancialRecords() {
        // Arrange
        var mockConfigService = Substitute.For<IConfigurationService>();
        var mockLogger = Substitute.For<ILoggerService>();
        var mockGameEventService = Substitute.For<IGameEventService>();
        var mockProvider = Substitute.For<IMarketListingProvider>();
        var mockDataManager = Substitute.For<IDataManager>();

        var pluginConfig = new PluginConfiguration();
        var charData = new CharacterFinancialData {
            CharacterName = "Almeris",
            HomeWorldId = 33
        };

        var retData = new RetainerFinancialData {
            RetainerId = 100
        };
        retData.MarketListings.Add(0, new RetainerMarketListingSaveData { ItemId = 5000, Quantity = 1, PricePerUnit = 100 });
        charData.Retainers.Add(100, retData);

        pluginConfig.FinancialRecords.Add("Almeris_33", charData);

        mockConfigService.GetConfig().Returns(pluginConfig);
        mockDataManager.GetExcelSheet<Item>().Returns((ExcelSheet<Item>?)null);

        var service = new MarketListingTrackerService(mockConfigService, mockGameEventService, mockProvider, mockDataManager, mockLogger);

        // Act
        var resultForRetainer100 = service.GetListingsForRetainer(100);

        // Assert
        Assert.Single(resultForRetainer100);
        Assert.Equal(5000u, resultForRetainer100[0].ItemId);
        Assert.Equal("Unknown Item", resultForRetainer100[0].ItemName);
    }
}