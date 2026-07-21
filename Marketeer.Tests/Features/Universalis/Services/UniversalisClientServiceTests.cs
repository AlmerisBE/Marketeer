using Marketeer.Features.Logging.Contracts;
using Marketeer.Features.Universalis.Services;
using NSubstitute;
using System.Net;
using Xunit;

namespace Marketeer.Tests.Features.Universalis.Services;

public class UniversalisClientServiceTests {

    [Fact]
    public async Task GetLowestPricesAsync_WithMultipleItems_ParsesDictionaryAndReturnsResults() {
        // Arrange
        var mockLogger = Substitute.For<ILoggerService>();

        var jsonResponse = @"{
            ""items"": {
                ""1234"": {
                    ""itemID"": 1234,
                    ""listings"": [
                        { ""pricePerUnit"": 600, ""retainerName"": ""ExpensiveRetainer"" },
                        { ""pricePerUnit"": 500, ""retainerName"": ""CheapRetainer"" }
                    ]
                },
                ""5678"": {
                    ""itemID"": 5678,
                    ""listings"": [
                        { ""pricePerUnit"": 1000, ""retainerName"": ""SoloRetainer"" }
                    ]
                }
            }
        }";

        var mockHandler = new MockHttpMessageHandler(jsonResponse, HttpStatusCode.OK);
        var httpClient = new HttpClient(mockHandler) {
            BaseAddress = new Uri("https://universalis.app/api/v2/")
        };

        var service = new UniversalisClientService(httpClient, mockLogger);

        // Act
        var results = await service.GetLowestPricesAsync(new[] { 1234u, 5678u }, 33);

        // Assert
        Assert.NotNull(results);
        Assert.Equal(2, results.Count);

        Assert.Contains(results, r => r.ItemId == 1234u && r.Price == 500u && r.RetainerName == "CheapRetainer");
        Assert.Contains(results, r => r.ItemId == 5678u && r.Price == 1000u && r.RetainerName == "SoloRetainer");
    }
}