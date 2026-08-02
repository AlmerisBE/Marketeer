namespace Marketeer.API.MarketWatch.Models;

public class WatchedItem {
    public uint ItemId { get; set; }

    public uint? TargetBuyPrice { get; set; }
    public uint? TargetSellPrice { get; set; }

    // Computed properties replace the physical booleans
    public bool IsBuyWatchEnabled => this.TargetBuyPrice.HasValue && this.TargetBuyPrice.Value > 0;
    public bool IsSellWatchEnabled => this.TargetSellPrice.HasValue && this.TargetSellPrice.Value > 0;

    public bool IsEligibleForPolling() {
        return this.IsBuyWatchEnabled || this.IsSellWatchEnabled;
    }
}