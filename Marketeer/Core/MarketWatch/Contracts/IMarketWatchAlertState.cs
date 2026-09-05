using Marketeer.Core.MarketWatch.Models;
using System;
using System.Collections.Generic;

namespace Marketeer.Core.MarketWatch.Contracts;

public interface IMarketWatchAlertState {
    IReadOnlyList<MarketWatchAlert> LatestAlerts { get; }
    DateTime LastUpdate { get; }

    void UpdateAlerts(IEnumerable<MarketWatchAlert> alerts);
}