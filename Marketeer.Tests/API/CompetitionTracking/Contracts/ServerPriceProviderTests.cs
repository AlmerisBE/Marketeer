using Dalamud.Plugin.Services;
using Marketeer.API.Universalis.Contracts;
using Marketeer.API.Universalis.Models;
using Marketeer.Core.Configuration.Contracts;
using Marketeer.Core.Configuration.Models;
using Marketeer.Core.Logging.Contracts;
using Marketeer.Core.MarketPricing.Contracts;
using Marketeer.Core.MarketPricing.Services;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.API.CompetitionTracking.Contracts;

public class ServerPriceProviderTests {
    [Fact]
    public async Task GetPricingsAsync_ReturnsCachedData_IfValid() {
        var client = Substitute.For<IUniversalisClient>();
        var updateMutator = Substitute.For<IUniversalisUpdateMutator>();
        var config = Substitute.For<IConfigurationService>();
        var framework = Substitute.For<IFramework>();
        var logger = Substitute.For<ILoggerService>();

        config.GetConfig().Returns(new PluginConfiguration { UniversalisCacheMinutes = 30 });

        var service = new MarketPriceCacheService(client, updateMutator, config, framework, logger);

        // Pre-fill the cache using the local update method
        service.UpdateLocalPrices(123u, 73u, new List<LowestPriceResult> {
            new LowestPriceResult { Price = 500, RetainerName = "Test" }
        });

        // Querying should hit the local cache, not the API
        var results = await service.GetPricingsAsync(new[] { 123u }, 73u, bypassCache: false);

        Assert.Single(results);
        Assert.Single(results[0].Listings);
        Assert.Equal(500u, results[0].Listings[0].Price);

        // Ensure API was not called
        await client.DidNotReceive().FetchDataAsync(Arg.Any<IEnumerable<uint>>(), Arg.Any<uint>());
    }
}