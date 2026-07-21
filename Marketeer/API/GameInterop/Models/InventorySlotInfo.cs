namespace Marketeer.API.GameInterop.Models;

public class InventorySlotInfo {
    public uint SlotIndex { get; set; }
    public bool IsOccupied { get; set; }
    public uint ItemId { get; set; }
    public uint Quantity { get; set; }
    public uint PricePerUnit { get; set; }
}