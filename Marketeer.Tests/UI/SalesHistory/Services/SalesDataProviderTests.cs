using Marketeer.API.SalesHistory.Contracts;
using Marketeer.API.SalesHistory.Models;
using Marketeer.Core.SalesHistory.Services;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.UI.SalesHistory.Services;

public class SalesDataProviderTests {
    [Fact]
    public void GetSalesData_WithExistingSales_ReturnsAggregatedViewRecords() {
        // Arrange
        var mockRepo = Substitute.For<ISalesRepository>();
        var mockAnalysis = Substitute.For<ISalesAnalysisService>();
        var mockResolver = Substitute.For<IItemResolverService>();

        var sales = new List<SaleRecord> {
            new SaleRecord { ItemId = 10, Quantity = 5, UnitPrice = 100, SaleDate = new DateTime(2023, 1, 1) },
            new SaleRecord { ItemId = 10, Quantity = 5, UnitPrice = 120, SaleDate = new DateTime(2023, 1, 2) }
        };

        mockRepo.GetAllSales().Returns(sales);

        var summary = new ItemSalesSummary {
            ItemId = 10,
            TotalQuantitySold = 10,
            AverageUnitPrice = 110,
            TotalRevenue = 1100
        };
        mockAnalysis.AnalyzeItem(10, Arg.Any<IReadOnlyList<SaleRecord>>()).Returns(summary);

        mockResolver.ResolveItemName(10).Returns("Potion");
        mockResolver.ResolveIconId(10).Returns(42u);

        var provider = new SalesDataProvider(mockRepo, mockAnalysis, mockResolver);

        // Act
        var results = provider.GetSalesData();

        // Assert
        Assert.Single(results);
        Assert.Equal(10u, results[0].ItemId);
        Assert.Equal("Potion", results[0].Name);
        Assert.Equal(42u, results[0].IconId);
        Assert.Equal(10u, results[0].TotalQuantitySold);
        Assert.Equal(110.0, results[0].AverageUnitPrice);
        Assert.Equal(1100ul, results[0].TotalRevenue);
        Assert.Equal(new DateTime(2023, 1, 2), results[0].LastSaleDate);
    }
}