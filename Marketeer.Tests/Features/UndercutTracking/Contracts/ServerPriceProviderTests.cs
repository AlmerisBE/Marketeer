using Marketeer.Features.UndercutTracking.Contracts;
using Marketeer.Features.UndercutTracking.Models;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Features.UndercutTracking.Contracts;

public class ServerPriceProviderTests {
    [Fact]
    public async Task GetLowestPriceAsync_ReturnsLowestPriceResult() {
        // Arrange
        var mockProvider = Substitute.For<IServerPriceProvider>();
        var expectedResult = new LowestPriceResult {
            ItemId = 1234,
            Price = 500,
            RetainerName = "Almeris"
        };

        mockProvider.GetLowestPriceAsync(1234, 33).Returns(Task.FromResult<LowestPriceResult?>(expectedResult));

        // Act
        var result = await mockProvider.GetLowestPriceAsync(1234, 33);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1234u, result!.ItemId);
        Assert.Equal(500u, result.Price);
        Assert.Equal("Almeris", result.RetainerName);
    }
}