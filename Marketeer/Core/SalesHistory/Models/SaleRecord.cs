using System;

namespace Marketeer.Core.SalesHistory.Models;

[Serializable]
public class SaleRecord {
    public ulong RetainerId { get; set; }
    public uint ItemId { get; set; }
    public uint Quantity { get; set; }
    public uint UnitPrice { get; set; }
    public string BuyerName { get; set; } = string.Empty;
    public DateTime SaleDate { get; set; }
    public DateTime ListingDate { get; set; }

    public override bool Equals(object? obj) {
        if (obj is SaleRecord other) {
            return this.ItemId == other.ItemId &&
                   this.Quantity == other.Quantity &&
                   this.UnitPrice == other.UnitPrice &&
                   this.BuyerName == other.BuyerName &&
                   this.SaleDate == other.SaleDate;
        }
        return false;
    }

    public override int GetHashCode() {
        return HashCode.Combine(this.ItemId, this.Quantity, this.UnitPrice, this.BuyerName, this.SaleDate);
    }
}