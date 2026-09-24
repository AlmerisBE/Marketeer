using Marketeer.Core.Configuration.Contracts;
using Marketeer.Core.Configuration.Models;
using Marketeer.Core.Logging.Contracts;
using Marketeer.Core.SalesHistory.Models;
using Marketeer.Core.SalesHistory.Services;
using Marketeer.Core.Storage.Contracts;
using Microsoft.Data.Sqlite;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Core.SalesHistory.Services;

public class SalesRepositoryTests : IDisposable {
    private IDatabaseService mockDb;
    private IConfigurationService mockConfig;
    private ILoggerService mockLogger;
    private SqliteConnection keepAliveConnection;

    public SalesRepositoryTests() {
        this.mockDb = Substitute.For<IDatabaseService>();
        this.mockConfig = Substitute.For<IConfigurationService>();
        this.mockLogger = Substitute.For<ILoggerService>();

        // Create a shared in-memory database string for cross-connection persistence during the test
        var connectionString = "Data Source=TestSalesDb;Mode=Memory;Cache=Shared";

        // Keep one connection open to ensure the in-memory database is not destroyed between calls
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
        var sale2 = new SaleRecord { RetainerId = 2, ItemId = 100, Quantity = 2, UnitPrice = 500, BuyerName = "John Doe", SaleDate = date, ListingDate = date }; // Retainer ID differs, but constraint ignores it

        repo.AddSales(new[] { sale1 });
        repo.AddSales(new[] { sale2 }); // Should be ignored based on UNIQUE(ItemId, Quantity, UnitPrice, BuyerName, SaleDate)

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

        // The constructor triggers InitializeTable and MigrateLegacyData
        var repo = new SalesRepository(this.mockDb, this.mockConfig, this.mockLogger);
        var results = repo.GetAllSales();

        Assert.Single(results);
        Assert.Equal(999u, results[0].ItemId);
        Assert.Empty(config.SalesHistory); // Verify legacy list was cleared
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
}