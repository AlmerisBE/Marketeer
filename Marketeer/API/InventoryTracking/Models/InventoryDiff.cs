using System.Collections.Generic;

namespace Marketeer.API.InventoryTracking.Models;

public class InventoryDiff {
    public List<TrackedItem> Added { get; set; } = new();
    public List<TrackedItem> Removed { get; set; } = new();
    public List<ItemMove> Moved { get; set; } = new();
    public List<ItemQuantityChange> QuantityChanged { get; set; } = new();
}

public class ItemMove {
    public uint ItemId { get; set; }
    public uint OldContainerId { get; set; }
    public int OldSlotIndex { get; set; }
    public uint NewContainerId { get; set; }
    public int NewSlotIndex { get; set; }
}

public class ItemQuantityChange {
    public uint ItemId { get; set; }
    public uint ContainerId { get; set; }
    public int SlotIndex { get; set; }
    public int Difference { get; set; }
}