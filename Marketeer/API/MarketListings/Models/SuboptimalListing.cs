namespace Marketeer.API.MarketListings.Models;

public class SuboptimalListing {
    public uint ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string CharacterName { get; set; } = string.Empty;
    public string RetainerName { get; set; } = string.Empty;
    public uint Price { get; set; }
    public uint VendorPrice { get; set; }
}