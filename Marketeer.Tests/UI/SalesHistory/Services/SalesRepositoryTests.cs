using Marketeer.API.Configuration.Contracts;
using Marketeer.API.Configuration.Models;
using Marketeer.API.SalesHistory.Models;
using Marketeer.Core.SalesHistory.Services;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.UI.SalesHistory.Services;

public class SalesRepositoryTests {
    [Fact]
    public void AddSales_IgnoresDuplicatesAndSavesConfiguration() {
        // Arrange
        var mockConfigService = Substitute.For<IConfigurationService>();
        var config = new PluginConfiguration();
        mockConfigService.GetConfig().Returns(config);

        var repository = new SalesRepository(mockConfigService);

        var date = DateTime.UtcNow;
        var sale1 = new SaleRecord { ItemId = 1, Quantity = 1, UnitPrice = 100, BuyerName = "John", SaleDate = date };
        var sale2 = new SaleRecord { ItemId = 1, Quantity = 1, UnitPrice = 100, BuyerName = "John", SaleDate = date }; // Exact duplicate

        // Act
        repository.AddSales(new List<SaleRecord> { sale1, sale2 });

        // Assert
        Assert.Single(repository.GetAllSales());
        mockConfigService.Received(1).Save();
    }

    [Fact]
    public void ClearSales_WhenSalesExist_EmptiesCollectionAndSavesConfig() {
        // Arrange
        var mockConfigService = Substitute.For<IConfigurationService>();
        var config = new PluginConfiguration();

        var date = DateTime.UtcNow;
        config.SalesHistory.Add(new SaleRecord { ItemId = 1, Quantity = 1, UnitPrice = 100, BuyerName = "John", SaleDate = date });

        mockConfigService.GetConfig().Returns(config);

        var repository = new SalesRepository(mockConfigService);

        // Act
        repository.ClearSales();

        // Assert
        Assert.Empty(repository.GetAllSales());
        Assert.Empty(config.SalesHistory);
        mockConfigService.Received(1).Save();
    }
}