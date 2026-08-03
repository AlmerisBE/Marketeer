namespace Marketeer.API.Universalis.Models;

public class LowestPriceResult {
    public uint ItemId { get; set; }
    public uint Price { get; set; }
    public string RetainerName { get; set; } = string.Empty;
    public bool IsHq { get; set; }
}