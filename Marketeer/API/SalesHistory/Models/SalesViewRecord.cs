using System;

namespace Marketeer.API.SalesHistory.Models;

public class SalesViewRecord : ISalesViewRecord {
    public uint ItemId { get; set; }
    public uint IconId { get; set; }
    public string Name { get; set; } = string.Empty;
    public uint TotalQuantitySold { get; set; }
    public double AverageUnitPrice { get; set; }
    public ulong TotalRevenue { get; set; }
    public DateTime LastSaleDate { get; set; }
}