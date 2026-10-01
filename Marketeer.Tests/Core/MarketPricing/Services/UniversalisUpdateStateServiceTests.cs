using Marketeer.Core.MarketPricing.Services;
using Xunit;

namespace Marketeer.Tests.Core.MarketPricing.Services;

public class UniversalisUpdateStateServiceTests {
    [Fact]
    public void SetUpdating_TogglesUpdatingStateWithoutAffectingTime() {
        var service = new UniversalisUpdateStateService();

        Assert.False(service.IsUpdating);

        service.SetUpdating(true);
        Assert.True(service.IsUpdating);
        Assert.Null(service.LastUpdateTime);
    }

    [Fact]
    public void RecordSuccessfulUpdate_ResetsUpdatingStateAndSetsTimestamp() {
        var service = new UniversalisUpdateStateService();
        service.SetUpdating(true);

        service.RecordSuccessfulUpdate();

        Assert.False(service.IsUpdating);
        Assert.NotNull(service.LastUpdateTime);

        // Assert the timestamp is recent
        var diff = DateTime.UtcNow - service.LastUpdateTime.Value;
        Assert.True(diff.TotalSeconds < 2);
    }
}