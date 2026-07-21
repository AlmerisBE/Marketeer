using Marketeer.Features.Localization.Contracts;
using Marketeer.Features.UndercutTracking.UI;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Features.UndercutTracking.UI;

public class UndercutsTabTests {
    [Fact]
    public void Name_ReturnsTranslatedTabName() {
        // Arrange
        var mockLocalization = Substitute.For<ILocalizationService>();
        mockLocalization.Translate("Undercuts_TabName").Returns("Undercuts");

        var tab = new UndercutsTab(mockLocalization);

        // Act
        var result = tab.Name;

        // Assert
        Assert.Equal("Undercuts", result);
        Assert.Equal(40, tab.Priority);
    }
}