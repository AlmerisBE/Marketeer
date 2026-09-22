using Dalamud.Plugin;
using Marketeer.Core.Logging.Contracts;
using Marketeer.Core.Storage.Services;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Core.Storage.Services;

public class DatabaseServiceTests {
    [Fact]
    public void CreateConnection_ShouldReturnValidConnection_WithCorrectPath() {
        var pluginInterface = Substitute.For<IDalamudPluginInterface>();
        var logger = Substitute.For<ILoggerService>();

        // Mocking the ConfigDirectory
        var tempPath = Path.GetTempPath();
        var dirInfo = new DirectoryInfo(tempPath);
        pluginInterface.ConfigDirectory.Returns(dirInfo);

        var service = new DatabaseService(pluginInterface, logger);
        using var connection = service.CreateConnection();

        Assert.NotNull(connection);
        Assert.Contains("marketeer_data.db", connection.ConnectionString);
        Assert.Contains(tempPath, connection.ConnectionString);
    }
}