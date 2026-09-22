using Dalamud.Plugin;
using Marketeer.Core.Logging.Contracts;
using Marketeer.Core.Storage.Contracts;
using Microsoft.Data.Sqlite;
using System;
using System.IO;

namespace Marketeer.Core.Storage.Services;

public class DatabaseService : IDatabaseService {
    private string databasePath;
    private ILoggerService logger;

    public DatabaseService(IDalamudPluginInterface pluginInterface, ILoggerService logger) {
        this.logger = logger;
        this.databasePath = Path.Combine(pluginInterface.ConfigDirectory.FullName, "marketeer_data.db");
    }

    public void InitializeDatabase() {
        try {
            // Creating a connection automatically creates the file if it doesn't exist
            using var connection = this.CreateConnection();
            connection.Open();

            // Enable Write-Ahead Logging (WAL) for better concurrency and performance
            using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA journal_mode = 'wal';";
            command.ExecuteNonQuery();

            this.logger.Info($"Database initialized successfully at {this.databasePath}.");
        }
        catch (Exception ex) {
            this.logger.Error(ex, "Failed to initialize the SQLite database.");
            throw;
        }
    }

    public SqliteConnection CreateConnection() {
        return new SqliteConnection($"Data Source={this.databasePath}");
    }
}