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
                // Store DateTime as Ticks (INTEGER) for flawless native SQLite sorting and precision
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