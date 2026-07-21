using Marketeer.API.Universalis.Contracts;
using Marketeer.API.Universalis.Models;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.API.CompetitionTracking.Contracts;

public class ServerPriceProviderTests {

    [Fact]
    public async Task GetLowestPricesAsync_ReturnsListOfResults() {
        // Arrange
        var mockProvider = Substitute.For<IServerPriceProvider>();

        var expectedResults = new List<LowestPriceResult> {
            new LowestPriceResult { ItemId = 1234, Price = 500, RetainerName = "Almeris" },
            new LowestPriceResult { ItemId = 5678, Price = 1000, RetainerName = "Tester" }
        };

        mockProvider.GetLowestPricesAsync(Arg.Any<IEnumerable<uint>>(), 33).Returns(Task.FromResult<IReadOnlyList<LowestPriceResult>>(expectedResults));

        // Act
        var results = await mockProvider.GetLowestPricesAsync(new[] { 1234u, 5678u }, 33);

        // Assert
        Assert.NotNull(results);
        Assert.Equal(2, results.Count);
        Assert.Equal(500u, results[0].Price);
    }
}