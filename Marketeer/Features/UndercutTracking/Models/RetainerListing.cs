namespace Marketeer.Features.UndercutTracking.Models;

public class RetainerListing {
    public int SlotIndex { get; set; }
    public uint ItemId { get; set; }
    public uint Quantity { get; set; }
    public string RetainerName { get; set; } = string.Empty;
    public uint CurrentPrice { get; set; }
}