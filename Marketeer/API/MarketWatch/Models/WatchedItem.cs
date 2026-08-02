namespace Marketeer.API.MarketWatch.Models;

public class WatchedItem {
    public uint ItemId { get; set; }

    // Storing the target prices. Null means it's not configured yet.
    public uint? TargetBuyPrice { get; set; }
    public uint? TargetSellPrice { get; set; }

    public bool IsBuyWatchEnabled { get; set; }
    public bool IsSellWatchEnabled { get; set; }

    // Business logic to determine if the item is eligible for background polling
    public bool IsEligibleForPolling() {
        if (this.IsBuyWatchEnabled && this.TargetBuyPrice.HasValue) {
            return true;
        }

        if (this.IsSellWatchEnabled && this.TargetSellPrice.HasValue) {
            return true;
        }

        return false;
    }
}