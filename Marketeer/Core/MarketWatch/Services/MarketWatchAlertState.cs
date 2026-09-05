using Marketeer.Core.MarketWatch.Contracts;
using Marketeer.Core.MarketWatch.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Marketeer.Core.MarketWatch.Services;

public class MarketWatchAlertState : IMarketWatchAlertState {
    private List<MarketWatchAlert> latestAlerts = new();

    public IReadOnlyList<MarketWatchAlert> LatestAlerts => this.latestAlerts.AsReadOnly();
    public DateTime LastUpdate { get; private set; } = DateTime.MinValue;

    public void UpdateAlerts(IEnumerable<MarketWatchAlert> alerts) {
        this.latestAlerts = alerts.ToList();
        this.LastUpdate = DateTime.Now;
    }
}