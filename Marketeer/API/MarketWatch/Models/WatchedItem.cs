using System.Text.Json.Serialization;

namespace Marketeer.API.MarketWatch.Models;

public class WatchedItem {
    public uint ItemId { get; set; }
    public bool IsHighQuality { get; set; }

    public uint? TargetBuyPrice { get; set; }
    public uint? TargetSellPrice { get; set; }

    // Per-item chat notification toggle
    public bool EnableNotifications { get; set; } = true;

    [JsonIgnore]
    public string Key => $"{this.ItemId}_{(this.IsHighQuality ? "HQ" : "NQ")}";

    public bool IsBuyWatchEnabled => this.TargetBuyPrice.HasValue && this.TargetBuyPrice.Value > 0;
    public bool IsSellWatchEnabled => this.TargetSellPrice.HasValue && this.TargetSellPrice.Value > 0;

    public bool IsEligibleForPolling() {
        return this.IsBuyWatchEnabled || this.IsSellWatchEnabled;
    }
}