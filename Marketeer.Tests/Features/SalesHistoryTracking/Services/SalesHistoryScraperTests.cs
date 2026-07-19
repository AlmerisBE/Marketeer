using Marketeer.Features.Logging.Contracts;
using Marketeer.Features.SalesHistoryTracking.Contracts;
using Marketeer.Features.SalesHistoryTracking.Services;
using Marketeer.Features.WindowAbstraction.Contracts;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Features.SalesHistoryTracking.Services;

public class SalesHistoryScraperTests {
    [Fact]
    public void IsHistoryWindowOpen_WhenWindowIsVisible_ReturnsTrue() {
        // Arrange
        var mockWindowService = Substitute.For<INativeWindowService>();
        var mockItemResolver = Substitute.For<IItemResolverService>();
        var mockLogger = Substitute.For<ILoggerService>();
        var mockWindow = Substitute.For<INativeWindow>();

        mockWindow.IsVisible.Returns(true);
        mockWindowService.GetWindow("RetainerItemHistory").Returns(mockWindow);

        var scraper = new SalesHistoryScraper(mockWindowService, mockItemResolver, mockLogger);

        // Act
        var result = scraper.IsHistoryWindowOpen();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ScrapeSales_WithValidNodes_ReturnsParsedSaleRecords() {
        // Arrange
        var mockWindowService = Substitute.For<INativeWindowService>();
        var mockItemResolver = Substitute.For<IItemResolverService>();
        var mockLogger = Substitute.For<ILoggerService>();
        var mockWindow = Substitute.For<INativeWindow>();

        // We simulate a repeating pattern found in the UI nodes (Name with Quantity, Unit Price, Buyer, Date)
        var elements = new List<INativeUiElement> {
            CreateMockElement("Potion  x5"), //  is the HQ symbol in FFXIV font
            CreateMockElement("100"),
            CreateMockElement("Almeris Tester"),
            CreateMockElement("2023-10-15 14:30:00") // Simplified date format for the test
        };

        mockWindow.IsVisible.Returns(true);
        mockWindow.GetElements().Returns(elements);
        mockWindowService.GetWindow("RetainerItemHistory").Returns(mockWindow);

        mockItemResolver.ResolveItemId("Potion").Returns(10u);

        var scraper = new SalesHistoryScraper(mockWindowService, mockItemResolver, mockLogger);

        // Act
        var results = scraper.ScrapeSales();

        // Assert
        Assert.Single(results);
        Assert.Equal(10u, results[0].ItemId);
        Assert.Equal(5u, results[0].Quantity);
        Assert.Equal(100u, results[0].UnitPrice);
        Assert.Equal("Almeris Tester", results[0].BuyerName);
    }

    private INativeUiElement CreateMockElement(string text) {
        var element = Substitute.For<INativeUiElement>();
        element.Type.Returns(NativeUiElementType.Text);
        element.Text.Returns(text);
        return element;
    }
}