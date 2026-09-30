using Marketeer.API.Universalis.Services;
using Marketeer.Core.Logging.Contracts;
using NSubstitute;
using System.Net;
using Xunit;

namespace Marketeer.Tests.API.Universalis.Services;

public class UniversalisClientServiceTests {
    [Fact]
    public async Task FetchDataAsync_WhenApiReturnsGatewayTimeout_RetriesAndEventuallySucceeds() {
        var jsonResponse = @"
        {
            ""itemID"": 1234,
            ""averagePriceNQ"": 650.0,
            ""averagePriceHQ"": 1200.0,
            ""nqSaleVelocity"": 5.2,
            ""hqSaleVelocity"": 1.1,
            ""listings"": [
                { ""pricePerUnit"": 500, ""retainerName"": ""TestRetainer"", ""hq"": false }
            ]
        }";

        var mockHandler = new MockHttpMessageHandler(null, HttpStatusCode.GatewayTimeout);
        mockHandler.EnqueueResponse(null, HttpStatusCode.GatewayTimeout);
        mockHandler.EnqueueResponse(jsonResponse, HttpStatusCode.OK);

        var logger = Substitute.For<ILoggerService>();

        // We inject the mock handler via the new optional constructor parameter
        var service = new UniversalisClientService(logger, mockHandler);

        // Act
        var results = await service.FetchDataAsync(new[] { 1234u }, 73);

        // Assert
        Assert.Single(results);
        var data = results[0];
        Assert.Equal(1234u, data.BaseItemId);
        Assert.Equal(650u, data.AveragePriceNq);
        Assert.Equal(5.2f, data.NqSaleVelocity);

        logger.Received(2).Warning(Arg.Is<string>(s => s.Contains("GatewayTimeout")));
    }

    [Fact]
    public async Task FetchDataAsync_WhenApiReturnsNotFound_ReturnsEmptySilently() {
        var mockHandler = new MockHttpMessageHandler(null, HttpStatusCode.NotFound);
        var logger = Substitute.For<ILoggerService>();

        var service = new UniversalisClientService(logger, mockHandler);

        // Act
        var results = await service.FetchDataAsync(new[] { 9999u }, 73);

        // Assert
        Assert.Empty(results);
        logger.DidNotReceive().Error(Arg.Any<string>());
        logger.DidNotReceive().Warning(Arg.Any<string>());
    }
}