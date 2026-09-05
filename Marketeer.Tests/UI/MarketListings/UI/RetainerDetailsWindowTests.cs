using Marketeer.Core.MarketListings.Contracts;
using Marketeer.Core.MarketListings.UI;
using Marketeer.UI.Localization.Contracts;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.UI.MarketListings.UI;

public class RetainerDetailsWindowTests {
    [Fact]
    public void OpenForRetainer_UpdatesWindowNameAndSetsIsOpenToTrue() {
        // Arrange
        var mockTrackerService = Substitute.For<IMarketListingTrackerService>();
        var mockLocalizationService = Substitute.For<ILocalizationService>();

        mockLocalizationService.Translate("RetainerDetails_Title", "Talilo").Returns("Details: Talilo");

        var window = new RetainerDetailsWindow(mockTrackerService, mockLocalizationService);

        // Act
        window.OpenForRetainer(999u, "Talilo");

        // Assert
        Assert.True(window.IsOpen);
        Assert.Equal("Details: Talilo", window.WindowName);
    }
}