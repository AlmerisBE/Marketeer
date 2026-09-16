using Marketeer.Core.CharacterManagement.Contracts;
using Marketeer.Core.CharacterManagement.Models;
using Marketeer.UI.CharacterManagement.UI;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.UI.CharacterManagement.Presenters;

public class RetainerDataPresenterTests {
    [Fact]
    public void GetRetainers_ReturnsMappedDisplayData() {
        // Arrange
        var mockService = Substitute.For<IRetainerTrackerService>();

        var retainers = new List<TrackedRetainer> {
            new TrackedRetainer { Name = "Retainer A", RetainerId = 1, Gil = 150000u },
            new TrackedRetainer { Name = "Retainer B", RetainerId = 2, Gil = 500u }
        };

        mockService.GetRetainersForCharacter("Test Char", 33u).Returns(retainers);

        var presenter = new RetainerDataPresenter(mockService);

        // Act
        var result = presenter.GetRetainers("Test Char", 33u);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(1ul, result[0].RetainerId);
        Assert.Equal("Retainer A", result[0].Name);
        Assert.Equal(150000u, result[0].Gil);

        Assert.Equal(2ul, result[1].RetainerId);
        Assert.Equal("Retainer B", result[1].Name);
        Assert.Equal(500u, result[1].Gil);
    }
}