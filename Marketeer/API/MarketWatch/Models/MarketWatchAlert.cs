namespace Marketeer.API.MarketWatch.Models;

public enum MarketWatchAlertType {
    BuyTargetReached,
    SellTargetReached
}

public class MarketWatchAlert {
    public uint ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public MarketWatchAlertType AlertType { get; set; }
    public uint TargetPrice { get; set; }
    public uint CurrentPrice { get; set; }
    public string RetainerName { get; set; } = string.Empty;
}