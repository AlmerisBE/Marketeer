using Microsoft.Data.Sqlite;

namespace Marketeer.Core.Storage.Contracts;

public interface IDatabaseService {
    SqliteConnection CreateConnection();
    void InitializeDatabase();
}