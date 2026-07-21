using Marketeer.Features.Logging.Contracts;
using Marketeer.Features.Universalis.Services;
using NSubstitute;
using System.Net;
using Xunit;

namespace Marketeer.Tests.Features.Universalis.Services;

public class UniversalisClientServiceTests {

    [Fact]
    public async Task GetLowestPriceAsync_WhenApiReturnsValidData_ReturnsLowestPriceResult() {
        // Arrange
        var mockLogger = Substitute.For<ILoggerService>();

        var jsonResponse = @"{
            ""itemID"": 1234,
            ""listings"": [
                { ""pricePerUnit"": 600, ""retainerName"": ""ExpensiveRetainer"" },
                { ""pricePerUnit"": 500, ""retainerName"": ""CheapRetainer"" }
            ]
        }";

        var mockHandler = new MockHttpMessageHandler(jsonResponse, HttpStatusCode.OK);
        var httpClient = new HttpClient(mockHandler) {
            BaseAddress = new Uri("https://universalis.app/api/v2/")
        };

        var service = new UniversalisClientService(httpClient, mockLogger);

        // Act
        var result = await service.GetLowestPriceAsync(1234, 33);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1234u, result!.ItemId);
        Assert.Equal(500u, result.Price);
        Assert.Equal("CheapRetainer", result.RetainerName); // Ensures OrderBy worked
    }

    [Fact]
    public async Task GetLowestPriceAsync_WhenApiFails_ReturnsNullAndLogsWarning() {
        // Arrange
        var mockLogger = Substitute.For<ILoggerService>();
        var mockHandler = new MockHttpMessageHandler("Not Found", HttpStatusCode.NotFound);
        var httpClient = new HttpClient(mockHandler) {
            BaseAddress = new Uri("https://universalis.app/api/v2/")
        };

        var service = new UniversalisClientService(httpClient, mockLogger);

        // Act
        var result = await service.GetLowestPriceAsync(1234, 33);

        // Assert
        Assert.Null(result);
        mockLogger.Received(1).Warning(Arg.Is<string>(s => s.Contains("returned NotFound")));
    }
}