using Marketeer.API.Universalis.Services;
using Marketeer.Core.Logging.Contracts;
using NSubstitute;
using System.Net;
using Xunit;

namespace Marketeer.Tests.API.Universalis.Services;

public class UniversalisClientServiceTests {
    [Fact]
    public void Constructor_InitializesCorrectly() {
        var httpClient = new HttpClient();
        var logger = Substitute.For<ILoggerService>();

        var service = new UniversalisClientService(httpClient, logger);

        Assert.NotNull(service);
    }

    [Fact]
    public async Task FetchPricesAsync_WithMultipleItems_ParsesDictionaryAndReturnsResults() {
        var mockLogger = Substitute.For<ILoggerService>();

        var jsonResponse = @"{
            ""items"": {
                ""1234"": {
                    ""itemID"": 1234,
                    ""listings"": [
                        { ""pricePerUnit"": 600, ""retainerName"": ""ExpensiveRetainer"", ""hq"": false },
                        { ""pricePerUnit"": 500, ""retainerName"": ""CheapRetainer"", ""hq"": true }
                    ]
                },
                ""5678"": {
                    ""itemID"": 5678,
                    ""listings"": [
                        { ""pricePerUnit"": 1000, ""retainerName"": ""SoloRetainer"", ""hq"": false }
                    ]
                }
            }
        }";

        var mockHandler = new MockHttpMessageHandler(jsonResponse, HttpStatusCode.OK);
        var httpClient = new HttpClient(mockHandler) {
            BaseAddress = new Uri("https://universalis.app/api/v2/")
        };

        var service = new UniversalisClientService(httpClient, mockLogger);

        var results = await service.FetchPricesAsync(new[] { 1234u, 5678u }, 33);

        Assert.NotNull(results);
        Assert.Equal(2, results.Count);

        Assert.Contains(results, r => r.ItemId == 1234u && r.Price == 500u && r.RetainerName == "CheapRetainer" && r.IsHq);
        Assert.Contains(results, r => r.ItemId == 5678u && r.Price == 1000u && r.RetainerName == "SoloRetainer" && !r.IsHq);
    }
}

public class MockHttpMessageHandler : HttpMessageHandler {
    private string response;
    private HttpStatusCode statusCode;

    public MockHttpMessageHandler(string response, HttpStatusCode statusCode) {
        this.response = response;
        this.statusCode = statusCode;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
        return Task.FromResult(new HttpResponseMessage {
            StatusCode = this.statusCode,
            Content = new StringContent(this.response)
        });
    }
}