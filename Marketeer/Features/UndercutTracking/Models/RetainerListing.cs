namespace Marketeer.Features.UndercutTracking.Models;

public class RetainerListing {
    public uint ItemId { get; set; }
    public string RetainerName { get; set; } = string.Empty;
    public uint CurrentPrice { get; set; }
}