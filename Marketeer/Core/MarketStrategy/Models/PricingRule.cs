namespace Marketeer.Core.MarketStrategy.Models;

public class PricingRule {
    public string GroupName { get; set; } = string.Empty;
    public bool? IgnoreHq { get; set; }
    public double? HqPremiumPercentage { get; set; }
    public UndercutMode? UndercutMode { get; set; }
    public uint? UndercutAmount { get; set; }
    public double? UndercutRelativePercentage { get; set; }
    public double? OutlierMultiplier { get; set; }
    public uint? OutlierMinimumFloor { get; set; }
}