namespace Marketeer.Core.Configuration.Models;

public class PricingRule {
    public UndercutMode? UndercutMode { get; set; }
    public uint? UndercutAmount { get; set; }
    public double? UndercutRelativePercentage { get; set; }
    public double? HqPremiumPercentage { get; set; }
    public bool? IgnoreHq { get; set; }
}