namespace Marketeer.API.SalesHistory.Models;

public class ItemSalesSummary {
    public uint ItemId { get; set; }
    public uint TotalQuantitySold { get; set; }
    public double AverageStackSize { get; set; }
    public double AverageUnitPrice { get; set; }
    public ulong TotalRevenue { get; set; }
    public double SalesPerDay { get; set; }
}