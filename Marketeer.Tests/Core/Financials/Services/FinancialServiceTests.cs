using Marketeer.API.Configuration.Contracts;
using Marketeer.API.Configuration.Models;
using Marketeer.API.Financials.Models;
using Marketeer.Core.Financials.Services;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Core.Financials.Services;

public class FinancialServiceTests {
    [Fact]
    public void FinancialService_GetFinancialSummary_AggregatesTotalsCorrectly() {
        // Arrange
        var mockConfigService = Substitute.For<IConfigurationService>();
        var dummyConfig = new PluginConfiguration();

        var char1 = new CharacterFinancialData {
            CharacterName = "Almeris",
            HomeWorldId = 33
        };

        var retainer1 = new RetainerFinancialData {
            GilHeld = 5000
        };
        retainer1.MarketListings.Add(0, new RetainerMarketListingSaveData { PricePerUnit = 1000, Quantity = 2 }); // 2000
        char1.Retainers.Add(100, retainer1);

        var char2 = new CharacterFinancialData {
            CharacterName = "Feven",
            HomeWorldId = 33
        };
        char2.Retainers.Add(200, new RetainerFinancialData { GilHeld = 10000 });

        // Using the deterministic composite keys to secure mapping
        dummyConfig.FinancialRecords.Add("Almeris_33", char1);
        dummyConfig.FinancialRecords.Add("Feven_33", char2);

        mockConfigService.GetConfig().Returns(dummyConfig);
        var service = new FinancialService(mockConfigService);

        // Act
        var summary = service.GetFinancialSummary();

        // Assert
        Assert.Equal(15000ul, summary.GrandTotalGil); // 5000 + 10000
        Assert.Equal(2000ul, summary.GrandTotalMarketValue); // 1000 * 2
        Assert.Equal(2, summary.Characters.Count);
    }
}