using Marketeer.Core.MarketWatch.Models;
using Marketeer.Core.MarketWatch.Services;
using Xunit;

namespace Marketeer.Tests.Core.MarketWatch.Services;

public class MarketWatchAlertStateTests {
    [Fact]
    public void UpdateAlerts_ShouldStoreAlertsAndRefreshTimestamp() {
        var state = new MarketWatchAlertState();
        var initialTime = state.LastUpdate;

        var alerts = new List<MarketWatchAlert> {
            new MarketWatchAlert { ItemId = 1, CurrentPrice = 100 }
        };

        state.UpdateAlerts(alerts);

        Assert.Single(state.LatestAlerts);
        Assert.Equal(100u, state.LatestAlerts[0].CurrentPrice);
        Assert.True(state.LastUpdate > initialTime);
    }
}