namespace Marketeer.Core.MarketStrategy.Models;

public class AnomalyReport {
    public bool IsAnomalyDetected { get; set; }
    public uint TrueMarketValue { get; set; }
    public uint CrashThresholdPrice { get; set; }
    public int DumpedItemCount { get; set; }
    public uint TotalBuyoutCost { get; set; }
    public uint PotentialGrossProfit { get; set; }
}