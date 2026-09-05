using System;

namespace Marketeer.Core.SalesHistory.Contracts;

public class SalesGlobalSummary {
    public int TotalSalesCount { get; set; }
    public uint TotalItemsSold { get; set; }
    public double AverageItemsPerSale { get; set; }
    public ulong TotalRevenue { get; set; }
    public double AverageRevenuePerSale { get; set; }
}

public class FastestSellingItem {
    public uint ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public TimeSpan AverageTimeToSell { get; set; }
    public int SalesCount { get; set; }
}

public class DailyChartData {
    public DateTime Date { get; set; }
    public int SalesCount { get; set; }
    public ulong Revenue { get; set; }
}