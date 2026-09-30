using Marketeer.API.Universalis.Services;
using Marketeer.Core.Logging.Contracts;
using NSubstitute;
using System.Net;
using Xunit;

namespace Marketeer.Tests.API.Universalis.Services;

public class UniversalisClientServiceTests {
    [Fact]
    public async Task FetchDataAsync_WhenApiReturnsSuccess_ReturnsParsedData() {
        var jsonResponse = @"
        {
            ""itemID"": 1234,
            ""averagePriceNQ"": 650.5,
            ""averagePriceHQ"": 1200.0,
            ""listings"": [
                { ""pricePerUnit"": 500, ""retainerName"": ""TestRetainer"", ""hq"": false }
            ]
        }";

        var mockHandler = new MockHttpMessageHandler(jsonResponse, HttpStatusCode.OK);
        var httpClient = new HttpClient(mockHandler) { BaseAddress = new Uri("https://test.local/") };
        var logger = Substitute.For<ILoggerService>();

        var service = new UniversalisClientService(httpClient, logger);

        var results = await service.FetchDataAsync(new[] { 1234u }, 73);

        Assert.Single(results);
        var data = results[0];
        Assert.Equal(1234u, data.BaseItemId);
        Assert.Equal(650u, data.AveragePriceNq);
        Assert.Equal(1200u, data.AveragePriceHq);

        Assert.Single(data.Listings);
        Assert.Equal(1234u, data.Listings[0].ItemId);
        Assert.Equal(500u, data.Listings[0].Price);
        Assert.False(data.Listings[0].IsHq);
    }

    [Fact]
    public async Task FetchDataAsync_WithMultipleItems_ParsesDictionaryAndReturnsResults() {
        var jsonResponse = @"
        {
            ""items"": {
                ""1234"": {
                    ""itemID"": 1234,
                    ""averagePriceNQ"": 650.0,
                    ""averagePriceHQ"": 500.0,
                    ""listings"": [
                        { ""pricePerUnit"": 600, ""retainerName"": ""ExpensiveRetainer"", ""hq"": false },
                        { ""pricePerUnit"": 500, ""retainerName"": ""CheapRetainer"", ""hq"": true }
                    ]
                },
                ""5678"": {
                    ""itemID"": 5678,
                    ""averagePriceNQ"": 1000.0,
                    ""averagePriceHQ"": 0.0,
                    ""listings"": [
                        { ""pricePerUnit"": 1000, ""retainerName"": ""SoloRetainer"", ""hq"": false }
                    ]
                }
            }
        }";

        var mockHandler = new MockHttpMessageHandler(jsonResponse, HttpStatusCode.OK);
        var httpClient = new HttpClient(mockHandler) { BaseAddress = new Uri("https://test.local/") };
        var logger = Substitute.For<ILoggerService>();

        var service = new UniversalisClientService(httpClient, logger);

        var results = await service.FetchDataAsync(new[] { 1234u, 5678u }, 73);

        Assert.Equal(2, results.Count);

        var item1 = results.FirstOrDefault(r => r.BaseItemId == 1234u);
        Assert.NotNull(item1);
        Assert.Equal(650u, item1.AveragePriceNq);
        Assert.Contains(item1.Listings, r => r.ItemId == 1234u && r.Price == 600u && !r.IsHq);
        Assert.Contains(item1.Listings, r => r.ItemId == 1001234u && r.Price == 500u && r.IsHq); // HQ offset validated

        var item2 = results.FirstOrDefault(r => r.BaseItemId == 5678u);
        Assert.NotNull(item2);
        Assert.Contains(item2.Listings, r => r.ItemId == 5678u && r.Price == 1000u && !r.IsHq);
    }

    [Fact]
    public async Task FetchDataAsync_WhenApiReturnsGatewayTimeout_RetriesAndEventuallySucceeds() {
        var jsonResponse = @"
        {
            ""itemID"": 1234,
            ""averagePriceNQ"": 650.0,
            ""averagePriceHQ"": 1200.0,
            ""listings"": [
                { ""pricePerUnit"": 500, ""retainerName"": ""TestRetainer"", ""hq"": false }
            ]
        }";

        // Setup the mock to fail twice with 504, then succeed with 200 OK on the third attempt
        var mockHandler = new MockHttpMessageHandler(null, HttpStatusCode.GatewayTimeout);
        mockHandler.EnqueueResponse(null, HttpStatusCode.GatewayTimeout);
        mockHandler.EnqueueResponse(jsonResponse, HttpStatusCode.OK);

        var httpClient = new HttpClient(mockHandler) { BaseAddress = new Uri("https://test.local/") };
        var logger = Substitute.For<ILoggerService>();

        var service = new UniversalisClientService(httpClient, logger);

        // Act
        var results = await service.FetchDataAsync(new[] { 1234u }, 73);

        // Assert
        Assert.Single(results);
        var data = results[0];
        Assert.Equal(1234u, data.BaseItemId);
        Assert.Equal(650u, data.AveragePriceNq);

        // Verify that the logger recorded the retries
        logger.Received(2).Warning(Arg.Is<string>(s => s.Contains("GatewayTimeout")));
    }

    [Fact]
    public async Task FetchDataAsync_WhenApiReturnsNotFound_ReturnsEmptySilently() {
        var mockHandler = new MockHttpMessageHandler(null, HttpStatusCode.NotFound);
        var httpClient = new HttpClient(mockHandler) { BaseAddress = new Uri("https://test.local/") };
        var logger = Substitute.For<ILoggerService>();

        var service = new UniversalisClientService(httpClient, logger);

        // Act
        var results = await service.FetchDataAsync(new[] { 9999u }, 73);

        // Assert
        Assert.Empty(results);

        // Ensure no false-positive errors are logged for a naturally empty market
        logger.DidNotReceive().Error(Arg.Any<string>());
        logger.DidNotReceive().Warning(Arg.Any<string>());
    }
}