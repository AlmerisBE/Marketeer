using Dalamud.Plugin.Services;
using Marketeer.API.Universalis.Contracts;
using Marketeer.API.Universalis.Models;
using Marketeer.Core.Configuration.Contracts;
using Marketeer.Core.Configuration.Models;
using Marketeer.Core.Logging.Contracts;
using Marketeer.Core.MarketPricing.Contracts;
using Marketeer.Core.MarketPricing.Services;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Marketeer.Tests.Core.MarketPricing.Services;

public class MarketPriceCacheServiceTests {
    [Fact]
    public async Task GetPricingsAsync_WhenNotCached_FetchesFromApiAndMutatesState() {
        var client = Substitute.For<IUniversalisClient>();
        var mutator = Substitute.For<IUniversalisUpdateMutator>();
        var configService = Substitute.For<IConfigurationService>();
        var framework = Substitute.For<IFramework>();
        var logger = Substitute.For<ILoggerService>();

        configService.GetConfig().Returns(new PluginConfiguration { UniversalisCacheMinutes = 30 });

        client.FetchDataAsync(Arg.Any<IEnumerable<uint>>(), 33).Returns(new List<UniversalisItemData> {
            new UniversalisItemData { BaseItemId = 123, Listings = new List<LowestPriceResult>() }
        });

        var service = new MarketPriceCacheService(client, mutator, configService, framework, logger);

        // Act
        var results = await service.GetPricingsAsync(new[] { 123u }, 33);

        // Assert
        Assert.Single(results);
        Assert.Equal(123u, results.First().ItemId);

        Received.InOrder(() => {
            mutator.SetUpdating(true);
            client.FetchDataAsync(Arg.Is<IEnumerable<uint>>(x => x.Contains(123u)), 33);
            mutator.RecordSuccessfulUpdate();
        });
    }

    [Fact]
    public async Task GetPricingsAsync_WhenApiThrowsException_LogsWarningAndResetsState() {
        var client = Substitute.For<IUniversalisClient>();
        var mutator = Substitute.For<IUniversalisUpdateMutator>();
        var configService = Substitute.For<IConfigurationService>();
        var framework = Substitute.For<IFramework>();
        var logger = Substitute.For<ILoggerService>();

        configService.GetConfig().Returns(new PluginConfiguration { UniversalisCacheMinutes = 30 });
        client.FetchDataAsync(Arg.Any<IEnumerable<uint>>(), 33).ThrowsAsync(new Exception("Network timeout"));

        var service = new MarketPriceCacheService(client, mutator, configService, framework, logger);

        // Act
        var results = await service.GetPricingsAsync(new[] { 123u }, 33);

        // Assert
        Assert.Single(results); // Returns empty MarketItemPricing as fallback

        // Assert we log a Warning instead of an Error to prevent Dalamud fatal log pollution
        logger.Received(1).Warning(Arg.Is<string>(s => s.Contains("Failed to fetch fallback API prices")));
        logger.DidNotReceiveWithAnyArgs().Error(Arg.Any<Exception>(), Arg.Any<string>());

        Received.InOrder(() => {
            mutator.SetUpdating(true);
            client.FetchDataAsync(Arg.Any<IEnumerable<uint>>(), 33);
            mutator.SetUpdating(false);
        });
    }

    [Fact]
    public async Task GetPricingsAsync_WhenCalledConcurrently_OnlyFetchesFromApiOnce() {
        var client = Substitute.For<IUniversalisClient>();
        var mutator = Substitute.For<IUniversalisUpdateMutator>();
        var configService = Substitute.For<IConfigurationService>();
        var framework = Substitute.For<IFramework>();
        var logger = Substitute.For<ILoggerService>();

        configService.GetConfig().Returns(new PluginConfiguration { UniversalisCacheMinutes = 30 });

        // Local function to explicitly return the correct Task signature for NSubstitute
        async Task<IReadOnlyList<UniversalisItemData>> GetDelayedDataAsync() {
            await Task.Delay(50);
            return new List<UniversalisItemData> {
                new UniversalisItemData { BaseItemId = 123, Listings = new List<LowestPriceResult>() }
            };
        }

        client.FetchDataAsync(Arg.Any<IEnumerable<uint>>(), 33).Returns(_ => GetDelayedDataAsync());

        var service = new MarketPriceCacheService(client, mutator, configService, framework, logger);

        // Act
        var task1 = service.GetPricingsAsync(new[] { 123u }, 33);
        var task2 = service.GetPricingsAsync(new[] { 123u }, 33);
        var task3 = service.GetPricingsAsync(new[] { 123u }, 33);

        await Task.WhenAll(task1, task2, task3);

        // Assert
        await client.Received(1).FetchDataAsync(Arg.Any<IEnumerable<uint>>(), 33);

        // Use await instead of .Result to satisfy xUnit1031 and prevent deadlocks
        Assert.Single(await task1);
        Assert.Single(await task2);
        Assert.Single(await task3);
    }
}