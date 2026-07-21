using System;

namespace Marketeer.API.SalesHistory.Models;

public interface ISalesViewRecord {
    uint ItemId { get; }
    uint IconId { get; }
    string Name { get; }
    uint TotalQuantitySold { get; }
    double AverageUnitPrice { get; }
    ulong TotalRevenue { get; }
    DateTime LastSaleDate { get; }
}