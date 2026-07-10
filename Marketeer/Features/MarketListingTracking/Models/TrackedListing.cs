using System;

namespace Marketeer.Features.MarketListingTracking.Models;

[Serializable]
public class TrackedListing {
    // Foreign key linking to the owner retainer
    public ulong AssociatedRetainerId { get; set; }

    public uint ItemId { get; set; }
    public uint Quantity { get; set; }
    public uint PricePerUnit { get; set; }

    public override bool Equals(object? obj) {
        if (obj is TrackedListing other) {
            return this.AssociatedRetainerId == other.AssociatedRetainerId
                && this.ItemId == other.ItemId
                && this.PricePerUnit == other.PricePerUnit;
        }
        return false;
    }

    public override int GetHashCode() {
        return HashCode.Combine(this.AssociatedRetainerId, this.ItemId, this.PricePerUnit);
    }
}