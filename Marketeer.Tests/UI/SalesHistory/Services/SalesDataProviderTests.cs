using Marketeer.Core.SalesHistory.Contracts;
using Marketeer.Core.SalesHistory.Models;
using Marketeer.Core.SalesHistory.Services;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Core.SalesHistory.Services;

public class SalesDataProviderTests {
    [Fact]
    public void GetSalesData_WithAllTimePeriod_ReturnsAggregatedSalesRecords() {
        // Arrange
        var mockRepo = Substitute.For<ISalesRepository>();
        var mockAnalysis = Substitute.For<ISalesAnalysisService>();
        var mockResolver = Substitute.For<IItemResolverService>();

        var sales = new List<SaleRecord> {
            new SaleRecord { ItemId = 1, Quantity = 2, UnitPrice = 500, SaleDate = DateTime.UtcNow }
        };

        mockRepo.GetAllSales().Returns(sales);

        mockAnalysis.AnalyzeItem(1, Arg.Any<IReadOnlyList<SaleRecord>>())
            .Returns(new ItemSalesSummary {
                ItemId = 1,
                TotalQuantitySold = 2,
                AverageUnitPrice = 500,
                TotalRevenue = 1000
            });

        mockResolver.ResolveItemName(1).Returns("Potion");
        mockResolver.ResolveIconId(1).Returns(42u);

        var provider = new SalesDataProvider(mockRepo, mockAnalysis, mockResolver);

        // Act
        var result = provider.GetSalesData(SalesPeriod.AllTime);

        // Assert
        Assert.Single(result);
        Assert.Equal("Potion", result[0].Name);
        Assert.Equal(1000ul, result[0].TotalRevenue);
    }

    [Fact]
    public void GetSalesData_WithTodayPeriod_FiltersOutOlderSales() {
        // Arrange
        var mockRepo = Substitute.For<ISalesRepository>();
        var mockAnalysis = Substitute.For<ISalesAnalysisService>();
        var mockResolver = Substitute.For<IItemResolverService>();

        var today = DateTime.UtcNow.Date;
        var rawSales = new List<SaleRecord> {
            new SaleRecord { ItemId = 100, Quantity = 1, UnitPrice = 1000, SaleDate = today },
            new SaleRecord { ItemId = 100, Quantity = 5, UnitPrice = 1000, SaleDate = today.AddDays(-10) }
        };

        mockRepo.GetAllSales().Returns(rawSales);

        // The analysis service should now be called with only 1 item (the one from today)
        mockAnalysis.AnalyzeItem(100, Arg.Is<IReadOnlyList<SaleRecord>>(list => list.Count == 1))
            .Returns(new ItemSalesSummary {
                ItemId = 100,
                TotalQuantitySold = 1,
                AverageUnitPrice = 1000,
                TotalRevenue = 1000
            });

        mockResolver.ResolveItemName(100).Returns("Ether");
        mockResolver.ResolveIconId(100).Returns(123u);

        var provider = new SalesDataProvider(mockRepo, mockAnalysis, mockResolver);

        // Act
        var result = provider.GetSalesData(SalesPeriod.Today);

        // Assert
        Assert.Single(result);
        Assert.Equal(100u, result[0].ItemId);
        Assert.Equal(1000ul, result[0].TotalRevenue);
    }
}