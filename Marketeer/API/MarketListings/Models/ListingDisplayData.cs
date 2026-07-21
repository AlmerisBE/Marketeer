namespace Marketeer.API.MarketListings.Models;

public class ListingDisplayData {
    public uint ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public uint Quantity { get; set; }
    public uint PricePerUnit { get; set; }
    public uint TotalPrice { get; set; }
    public uint Tax { get; set; }
}