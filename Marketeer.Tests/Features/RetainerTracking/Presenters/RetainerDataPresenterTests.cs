using Marketeer.Features.RetainerTracking.Contracts;
using Marketeer.Features.RetainerTracking.Presenters;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Features.RetainerTracking.Presenters;

public class RetainerDataPresenterTests {
    [Fact]
    public void GetRetainers_ReturnsMappedDisplayData() {
        // Arrange
        var mockService = Substitute.For<IRetainerTrackerService>();
        mockService.GetRetainersForCharacter("Test Char", 33).Returns([
            new() { Name = "Retainer A", RetainerId = 1 },
            new() { Name = "Retainer B", RetainerId = 2 }
        ]);

        var presenter = new RetainerDataPresenter(mockService);

        // Act
        var result = presenter.GetRetainers("Test Char", 33);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(1ul, result[0].RetainerId);
        Assert.Equal("Retainer A", result[0].Name);
        Assert.Equal(2ul, result[1].RetainerId);
        Assert.Equal("Retainer B", result[1].Name);
    }
}