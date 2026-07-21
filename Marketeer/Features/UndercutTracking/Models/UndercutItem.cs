namespace Marketeer.Features.UndercutTracking.Models;

public class UndercutItem {
    public uint ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string RetainerName { get; set; } = string.Empty;
    public uint OurPrice { get; set; }
    public uint ServerCheapestPrice { get; set; }
}