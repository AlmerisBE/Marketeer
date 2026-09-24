using Marketeer.Core.Configuration.Contracts;
using Marketeer.Core.Logging.Contracts;
using Marketeer.Core.SalesHistory.Contracts;
using Marketeer.Core.SalesHistory.Models;
using Marketeer.Core.Storage.Contracts;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;

namespace Marketeer.Core.SalesHistory.Services;

public class SalesRepository : ISalesRepository {
    private IDatabaseService databaseService;
    private IConfigurationService configService;
    private ILoggerService logger;

    public SalesRepository(IDatabaseService databaseService, IConfigurationService configService, ILoggerService logger) {
        this.databaseService = databaseService;
        this.configService = configService;
        this.logger = logger;

        this.InitializeTable();
        this.MigrateLegacyData();
    }

    private void InitializeTable() {
        try {
            using var connection = this.databaseService.CreateConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS SalesHistory (
                    RetainerId INTEGER,
                    ItemId INTEGER,
                    Quantity INTEGER,
                    UnitPrice INTEGER,
                    BuyerName TEXT,
                    SaleDate INTEGER,
                    ListingDate INTEGER,
                    UNIQUE(ItemId, Quantity, UnitPrice, BuyerName, SaleDate)
                );";
            command.ExecuteNonQuery();
        }
        catch (Exception ex) {
            this.logger.Error(ex, "Failed to initialize SalesHistory SQLite table.");
        }
    }

    private void MigrateLegacyData() {
        var config = this.configService.GetConfig();
        if (config.SalesHistory == null || config.SalesHistory.Count == 0) return;

        try {
            this.logger.Info($"Migrating {config.SalesHistory.Count} legacy sales records from JSON to SQLite.");
            this.AddSales(config.SalesHistory);

            config.SalesHistory.Clear();
            this.configService.Save();
            this.logger.Info("Legacy sales data migration completed successfully.");
        }
        catch (Exception ex) {
            this.logger.Error(ex, "Failed to migrate legacy sales data.");
        }
    }

    public void AddSales(IEnumerable<SaleRecord> newSales) {
        try {
            using var connection = this.databaseService.CreateConnection();
            connection.Open();

            using var transaction = connection.BeginTransaction();
            using var command = connection.CreateCommand();

            command.Transaction = transaction;
            command.CommandText = @"
                INSERT OR IGNORE INTO SalesHistory 
                (RetainerId, ItemId, Quantity, UnitPrice, BuyerName, SaleDate, ListingDate) 
                VALUES (@RetainerId, @ItemId, @Quantity, @UnitPrice, @BuyerName, @SaleDate, @ListingDate);";

            var pRetainerId = command.Parameters.Add("@RetainerId", SqliteType.Integer);
            var pItemId = command.Parameters.Add("@ItemId", SqliteType.Integer);
            var pQuantity = command.Parameters.Add("@Quantity", SqliteType.Integer);
            var pUnitPrice = command.Parameters.Add("@UnitPrice", SqliteType.Integer);
            var pBuyerName = command.Parameters.Add("@BuyerName", SqliteType.Text);
            var pSaleDate = command.Parameters.Add("@SaleDate", SqliteType.Integer);
            var pListingDate = command.Parameters.Add("@ListingDate", SqliteType.Integer);

            foreach (var sale in newSales) {
                pRetainerId.Value = (long)sale.RetainerId;
                pItemId.Value = (long)sale.ItemId;
                pQuantity.Value = (long)sale.Quantity;
                pUnitPrice.Value = (long)sale.UnitPrice;
                pBuyerName.Value = sale.BuyerName;
                pSaleDate.Value = sale.SaleDate.Ticks;
                pListingDate.Value = sale.ListingDate.Ticks;

                command.ExecuteNonQuery();
            }

            transaction.Commit();
        }
        catch (Exception ex) {
            this.logger.Error(ex, "Failed to insert bulk sales records into the database.");
        }
    }

    public IReadOnlyList<SaleRecord> GetAllSales() {
        var sales = new List<SaleRecord>();

        try {
            using var connection = this.databaseService.CreateConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT RetainerId, ItemId, Quantity, UnitPrice, BuyerName, SaleDate, ListingDate FROM SalesHistory;";

            using var reader = command.ExecuteReader();
            while (reader.Read()) {
                sales.Add(new SaleRecord {
                    RetainerId = (ulong)reader.GetInt64(0),
                    ItemId = (uint)reader.GetInt64(1),
                    Quantity = (uint)reader.GetInt64(2),
                    UnitPrice = (uint)reader.GetInt64(3),
                    BuyerName = reader.GetString(4),
                    SaleDate = new DateTime(reader.GetInt64(5), DateTimeKind.Local),
                    ListingDate = new DateTime(reader.GetInt64(6), DateTimeKind.Local)
                });
            }
        }
        catch (Exception ex) {
            this.logger.Error(ex, "Failed to retrieve sales records from the database.");
        }

        return sales.AsReadOnly();
    }

    public SalesGlobalSummary GetGlobalSummary() {
        try {
            using var connection = this.databaseService.CreateConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(1), SUM(Quantity), SUM(Quantity * UnitPrice) FROM SalesHistory;";

            using var reader = command.ExecuteReader();
            if (reader.Read() && !reader.IsDBNull(0) && reader.GetInt32(0) > 0) {
                int count = reader.GetInt32(0);
                uint totalItems = (uint)reader.GetInt64(1);
                ulong totalRevenue = (ulong)reader.GetInt64(2);

                return new SalesGlobalSummary {
                    TotalSalesCount = count,
                    TotalItemsSold = totalItems,
                    AverageItemsPerSale = (double)totalItems / count,
                    TotalRevenue = totalRevenue,
                    AverageRevenuePerSale = (double)totalRevenue / count
                };
            }
        }
        catch (Exception ex) {
            this.logger.Error(ex, "Failed to retrieve global sales summary.");
        }

        return new SalesGlobalSummary();
    }

    public IReadOnlyList<ItemSalesSummary> GetTopBestSellers(int limit) {
        var results = new List<ItemSalesSummary>();

        try {
            using var connection = this.databaseService.CreateConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT ItemId, SUM(Quantity), AVG(Quantity), AVG(UnitPrice), SUM(Quantity * UnitPrice), MIN(SaleDate), MAX(SaleDate)
                FROM SalesHistory
                GROUP BY ItemId
                ORDER BY SUM(Quantity * UnitPrice) DESC
                LIMIT @Limit;";
            command.Parameters.AddWithValue("@Limit", limit);

            using var reader = command.ExecuteReader();
            while (reader.Read()) {
                var minDate = new DateTime(reader.GetInt64(5), DateTimeKind.Local);
                var maxDate = new DateTime(reader.GetInt64(6), DateTimeKind.Local);
                var timespan = maxDate - minDate;
                var daysElapsed = timespan.TotalDays > 0 ? timespan.TotalDays : 1.0;
                var quantity = (uint)reader.GetInt64(1);

                results.Add(new ItemSalesSummary {
                    ItemId = (uint)reader.GetInt64(0),
                    TotalQuantitySold = quantity,
                    AverageStackSize = reader.GetDouble(2),
                    AverageUnitPrice = reader.GetDouble(3),
                    TotalRevenue = (ulong)reader.GetInt64(4),
                    SalesPerDay = quantity / daysElapsed
                });
            }
        }
        catch (Exception ex) {
            this.logger.Error(ex, "Failed to retrieve top best sellers.");
        }

        return results.AsReadOnly();
    }

    public IReadOnlyList<FastestSellingItem> GetFastestSellingItems(int limit) {
        var results = new List<FastestSellingItem>();

        try {
            using var connection = this.databaseService.CreateConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            long year2000Ticks = new DateTime(2000, 1, 1).Ticks;

            command.CommandText = @"
                SELECT ItemId, AVG(SaleDate - ListingDate), COUNT(1)
                FROM SalesHistory
                WHERE ListingDate > @Year2000 AND SaleDate > ListingDate
                GROUP BY ItemId
                ORDER BY AVG(SaleDate - ListingDate) ASC
                LIMIT @Limit;";

            command.Parameters.AddWithValue("@Year2000", year2000Ticks);
            command.Parameters.AddWithValue("@Limit", limit);

            using var reader = command.ExecuteReader();
            while (reader.Read()) {
                results.Add(new FastestSellingItem {
                    ItemId = (uint)reader.GetInt64(0),
                    AverageTimeToSell = TimeSpan.FromTicks((long)reader.GetDouble(1)),
                    SalesCount = reader.GetInt32(2)
                });
            }
        }
        catch (Exception ex) {
            this.logger.Error(ex, "Failed to retrieve fastest selling items.");
        }

        return results.AsReadOnly();
    }

    public IReadOnlyList<SaleRecord> GetSalesSince(DateTime cutoff) {
        var sales = new List<SaleRecord>();

        try {
            using var connection = this.databaseService.CreateConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT RetainerId, ItemId, Quantity, UnitPrice, BuyerName, SaleDate, ListingDate FROM SalesHistory WHERE SaleDate >= @Cutoff;";
            command.Parameters.AddWithValue("@Cutoff", cutoff.Ticks);

            using var reader = command.ExecuteReader();
            while (reader.Read()) {
                sales.Add(new SaleRecord {
                    RetainerId = (ulong)reader.GetInt64(0),
                    ItemId = (uint)reader.GetInt64(1),
                    Quantity = (uint)reader.GetInt64(2),
                    UnitPrice = (uint)reader.GetInt64(3),
                    BuyerName = reader.GetString(4),
                    SaleDate = new DateTime(reader.GetInt64(5), DateTimeKind.Local),
                    ListingDate = new DateTime(reader.GetInt64(6), DateTimeKind.Local)
                });
            }
        }
        catch (Exception ex) {
            this.logger.Error(ex, "Failed to retrieve recent sales.");
        }

        return sales.AsReadOnly();
    }

    public void ClearSales() {
        try {
            using var connection = this.databaseService.CreateConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM SalesHistory;";
            command.ExecuteNonQuery();
        }
        catch (Exception ex) {
            this.logger.Error(ex, "Failed to clear sales records from the database.");
        }
    }
}