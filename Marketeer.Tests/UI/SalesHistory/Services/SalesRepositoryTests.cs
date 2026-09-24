using Marketeer.Core.Configuration.Contracts;
using Marketeer.Core.Configuration.Models;
using Marketeer.Core.Logging.Contracts;
using Marketeer.Core.SalesHistory.Models;
using Marketeer.Core.SalesHistory.Services;
using Marketeer.Core.Storage.Contracts;
using Microsoft.Data.Sqlite;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.UI.SalesHistory.Services;

public class SalesRepositoryTests : IDisposable {
    private IDatabaseService mockDb;
    private IConfigurationService mockConfig;
    private ILoggerService mockLogger;
    private SqliteConnection keepAliveConnection;

    public SalesRepositoryTests() {
        this.mockDb = Substitute.For<IDatabaseService>();
        this.mockConfig = Substitute.For<IConfigurationService>();
        this.mockLogger = Substitute.For<ILoggerService>();

        // Generate a unique database name for each test instance to ensure strict TDD isolation
        var dbName = Guid.NewGuid().ToString();
        var connectionString = $"Data Source={dbName};Mode=Memory;Cache=Shared";

        this.keepAliveConnection = new SqliteConnection(connectionString);
        this.keepAliveConnection.Open();

        this.mockDb.CreateConnection().Returns(_ => new SqliteConnection(connectionString));
        this.mockConfig.GetConfig().Returns(new PluginConfiguration());
    }

    [Fact]
    public void AddSales_WhenGivenNewSales_InsertsIntoDatabase() {
        var repo = new SalesRepository(this.mockDb, this.mockConfig, this.mockLogger);
        var date = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Local);

        var sales = new List<SaleRecord> {
            new SaleRecord { RetainerId = 1, ItemId = 100, Quantity = 2, UnitPrice = 500, BuyerName = "John Doe", SaleDate = date, ListingDate = date }
        };

        repo.AddSales(sales);
        var results = repo.GetAllSales();

        Assert.Single(results);
        Assert.Equal(100u, results[0].ItemId);
        Assert.Equal("John Doe", results[0].BuyerName);
    }

    [Fact]
    public void AddSales_WhenGivenDuplicateSales_IgnoresDuplicatesSilently() {
        var repo = new SalesRepository(this.mockDb, this.mockConfig, this.mockLogger);
        var date = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Local);

        var sale1 = new SaleRecord { RetainerId = 1, ItemId = 100, Quantity = 2, UnitPrice = 500, BuyerName = "John Doe", SaleDate = date, ListingDate = date };
        var sale2 = new SaleRecord { RetainerId = 2, ItemId = 100, Quantity = 2, UnitPrice = 500, BuyerName = "John Doe", SaleDate = date, ListingDate = date };

        repo.AddSales(new[] { sale1 });
        repo.AddSales(new[] { sale2 });

        var results = repo.GetAllSales();

        Assert.Single(results);
        Assert.Equal(1ul, results[0].RetainerId);
    }

    [Fact]
    public void MigrateLegacyData_WhenJsonDataExists_MovesToSqliteAndClearsJson() {
        var config = new PluginConfiguration();
        var date = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Local);
        config.SalesHistory.Add(new SaleRecord { ItemId = 999, Quantity = 1, UnitPrice = 1000, BuyerName = "Legacy Buyer", SaleDate = date });

        this.mockConfig.GetConfig().Returns(config);

        var repo = new SalesRepository(this.mockDb, this.mockConfig, this.mockLogger);
        var results = repo.GetAllSales();

        Assert.Single(results);
        Assert.Equal(999u, results[0].ItemId);
        Assert.Empty(config.SalesHistory);
        this.mockConfig.Received(1).Save();
    }

    [Fact]
    public void ClearSales_RemovesAllRecordsFromDatabase() {
        var repo = new SalesRepository(this.mockDb, this.mockConfig, this.mockLogger);
        var date = DateTime.Now;

        repo.AddSales(new[] { new SaleRecord { ItemId = 100, Quantity = 1, UnitPrice = 500, BuyerName = "Test", SaleDate = date } });
        Assert.Single(repo.GetAllSales());

        repo.ClearSales();

        Assert.Empty(repo.GetAllSales());
    }

    public void Dispose() {
        this.keepAliveConnection.Close();
        this.keepAliveConnection.Dispose();
    }

    [Fact]
    public void GetGlobalSummary_ReturnsCorrectAggregates() {
        var repo = new SalesRepository(this.mockDb, this.mockConfig, this.mockLogger);
        var date = DateTime.Now;

        repo.AddSales(new[] {
            new SaleRecord { ItemId = 1, Quantity = 2, UnitPrice = 100, BuyerName = "Buyer1", SaleDate = date, ListingDate = date },
            new SaleRecord { ItemId = 2, Quantity = 3, UnitPrice = 200, BuyerName = "Buyer2", SaleDate = date, ListingDate = date }
        });

        var summary = repo.GetGlobalSummary();

        Assert.Equal(2, summary.TotalSalesCount);
        Assert.Equal(5u, summary.TotalItemsSold);
        Assert.Equal(800ul, summary.TotalRevenue); // (2*100) + (3*200) = 800
    }

    [Fact]
    public void GetFastestSellingItems_FiltersCorrectlyAndCalculatesAverages() {
        var repo = new SalesRepository(this.mockDb, this.mockConfig, this.mockLogger);
        var listingDate = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Local);
        var saleDate = listingDate.AddHours(2);

        repo.AddSales(new[] {
            new SaleRecord { ItemId = 100, Quantity = 1, UnitPrice = 500, BuyerName = "A", ListingDate = listingDate, SaleDate = saleDate },
            new SaleRecord { ItemId = 100, Quantity = 1, UnitPrice = 500, BuyerName = "B", ListingDate = listingDate, SaleDate = saleDate.AddHours(2) }, // Diff: 2h and 4h -> avg 3h
            new SaleRecord { ItemId = 999, Quantity = 1, UnitPrice = 500, BuyerName = "C", ListingDate = DateTime.MinValue, SaleDate = saleDate } // Should be ignored (legacy)
        });

        var fastest = repo.GetFastestSellingItems(10);

        Assert.Single(fastest);
        Assert.Equal(100u, fastest[0].ItemId);
        Assert.Equal(TimeSpan.FromHours(3), fastest[0].AverageTimeToSell);
        Assert.Equal(2, fastest[0].SalesCount);
    }
}