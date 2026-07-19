using Marketeer.Features.SalesHistoryTracking.Models;
using Marketeer.Features.SalesHistoryTracking.Services;
using Xunit;

namespace Marketeer.Tests.Features.SalesHistoryTracking.Services;

public class SalesAnalysisServiceTests {

    [Fact]
    public void AnalyzeItem_WithValidHistory_CalculatesStatisticsCorrectly() {
        // Arrange
        var service = new SalesAnalysisService();
        var now = DateTime.UtcNow;

        var history = new List<SaleRecord> {
            new SaleRecord { ItemId = 10, Quantity = 10, UnitPrice = 100, SaleDate = now.AddDays(-2) },
            new SaleRecord { ItemId = 10, Quantity = 5, UnitPrice = 120, SaleDate = now.AddDays(-1) },
            new SaleRecord { ItemId = 10, Quantity = 15, UnitPrice = 90, SaleDate = now }
        };

        // Act
        var result = service.AnalyzeItem(10, history);

        // Assert
        Assert.Equal(10u, result.ItemId);
        Assert.Equal(30u, result.TotalQuantitySold); // 10 + 5 + 15
        Assert.Equal(10.0, result.AverageStackSize); // 30 items / 3 transactions
        Assert.Equal(2950ul, result.TotalRevenue); // (10*100) + (5*120) + (15*90)
        Assert.Equal(98.33, result.AverageUnitPrice, 2); // 2950 total revenue / 30 items

        // Frequency: 30 items sold over exactly 2 days (max date - min date) = 15 items per day
        Assert.Equal(15.0, result.SalesPerDay);
    }

    [Fact]
    public void AnalyzeItem_WithEmptyHistory_ReturnsZeroedSummary() {
        // Arrange
        var service = new SalesAnalysisService();

        // Act
        var result = service.AnalyzeItem(10, new List<SaleRecord>());

        // Assert
        Assert.Equal(10u, result.ItemId);
        Assert.Equal(0u, result.TotalQuantitySold);
        Assert.Equal(0ul, result.TotalRevenue);
        Assert.Equal(0.0, result.AverageUnitPrice);
    }
}