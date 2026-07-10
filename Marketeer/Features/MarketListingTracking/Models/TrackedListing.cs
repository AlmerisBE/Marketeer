using System;

namespace Marketeer.Features.MarketListingTracking.Models;

[Serializable]
public class TrackedListing {
    public ulong AssociatedRetainerId { get; set; }
    public uint SlotIndex { get; set; }
    public uint ItemId { get; set; }
    public uint Quantity { get; set; }
    public uint PricePerUnit { get; set; }

    public override bool Equals(object? obj) {
        if (obj is TrackedListing other) {
            return this.AssociatedRetainerId == other.AssociatedRetainerId
                && this.SlotIndex == other.SlotIndex;
        }
        return false;
    }

    public override int GetHashCode() {
        return HashCode.Combine(this.AssociatedRetainerId, this.SlotIndex);
    }
}