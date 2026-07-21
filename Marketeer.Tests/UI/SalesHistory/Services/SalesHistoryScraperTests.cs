using Dalamud.Game.NativeWrapper;
using Dalamud.Plugin.Services;
using Marketeer.API.Logging.Contracts;
using Marketeer.API.SalesHistory.Contracts;
using Marketeer.Core.SalesHistory.Services;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.UI.SalesHistory.Services;

public class SalesHistoryScraperTests {
    [Fact]
    public void IsHistoryWindowOpen_WhenWindowPointerIsNull_ReturnsFalse() {
        // Arrange
        var mockGameGui = Substitute.For<IGameGui>();
        var mockItemResolver = Substitute.For<IItemResolverService>();
        var mockLogger = Substitute.For<ILoggerService>();

        // Fix CS1503: Return the expected AtkUnitBasePtr struct instead of IntPtr.Zero
        mockGameGui.GetAddonByName("RetainerHistory").Returns(default(AtkUnitBasePtr));

        var scraper = new SalesHistoryScraper(mockGameGui, mockItemResolver, mockLogger);

        // Act
        var result = scraper.IsHistoryWindowOpen();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ScrapeSales_WhenWindowPointerIsNull_ReturnsEmptyListAndLogsWarning() {
        // Arrange
        var mockGameGui = Substitute.For<IGameGui>();
        var mockItemResolver = Substitute.For<IItemResolverService>();
        var mockLogger = Substitute.For<ILoggerService>();

        // Fix CS1503: Return the expected AtkUnitBasePtr struct instead of IntPtr.Zero
        mockGameGui.GetAddonByName("RetainerHistory").Returns(default(AtkUnitBasePtr));

        var scraper = new SalesHistoryScraper(mockGameGui, mockItemResolver, mockLogger);

        // Act
        var results = scraper.ScrapeSales();

        // Assert
        Assert.Empty(results);
        mockLogger.Received(1).Warning("Cannot scrape sales: 'RetainerHistory' pointer is null.");
    }
}