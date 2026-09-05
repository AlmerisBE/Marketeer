using Dalamud.Plugin.Services;
using Marketeer.API.Universalis.Services;
using Marketeer.Core.Configuration.Contracts;
using Marketeer.Core.Configuration.Models;
using Marketeer.Core.Logging.Contracts;
using NSubstitute;
using System.Net;
using Xunit;

namespace Marketeer.Tests.Core.Universalis.Services;

public class UniversalisClientServiceTests {
    [Fact]
    public void Constructor_InitializesCorrectly() {
        var httpClient = new HttpClient();
        var logger = Substitute.For<ILoggerService>();
        var configService = Substitute.For<IConfigurationService>();
        var framework = Substitute.For<IFramework>();

        var config = new PluginConfiguration { UniversalisCacheMinutes = 30 };
        configService.GetConfig().Returns(config);

        // Ajout du paramètre à l'instanciation
        var service = new UniversalisClientService(httpClient, logger, configService, framework);

        Assert.NotNull(service);
    }

    [Fact]
    public async Task GetLowestPricesAsync_WithMultipleItems_ParsesDictionaryAndReturnsResults() {
        var mockLogger = Substitute.For<ILoggerService>();
        var mockConfigService = Substitute.For<IConfigurationService>();
        mockConfigService.GetConfig().Returns(new PluginConfiguration { UniversalisCacheMinutes = 30 });

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
        var framework = Substitute.For<IFramework>();

        var service = new UniversalisClientService(httpClient, mockLogger, mockConfigService, framework);

        var results = await service.GetLowestPricesAsync(new[] { 1234u, 5678u }, 33);

        Assert.NotNull(results);
        Assert.Equal(2, results.Count);

        Assert.Contains(results, r => r.ItemId == 1234u && r.Price == 500u && r.RetainerName == "CheapRetainer");
        Assert.Contains(results, r => r.ItemId == 5678u && r.Price == 1000u && r.RetainerName == "SoloRetainer");
    }
}