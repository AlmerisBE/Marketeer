using System;

namespace Marketeer.API.InventoryTracking.Models;

[Serializable]
public class TrackedItem {
    public uint ItemId { get; set; }
    public uint Quantity { get; set; }
    public uint ContainerId { get; set; }
    public int SlotIndex { get; set; }

    public bool EqualsLocation(TrackedItem other) {
        return this.ContainerId == other.ContainerId && this.SlotIndex == other.SlotIndex;
    }
}