namespace Marketeer.API.CompetitionTracking.Models;

public class UndercutItem {
    public int SlotIndex { get; set; }
    public uint ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public uint Quantity { get; set; }
    public string RetainerName { get; set; } = string.Empty;
    public uint OurPrice { get; set; }
    public uint ServerCheapestPrice { get; set; }
    public string CompetitorName { get; set; } = string.Empty;
    public string CharacterName { get; set; } = string.Empty;
}