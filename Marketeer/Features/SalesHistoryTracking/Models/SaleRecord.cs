using System;

namespace Marketeer.Features.SalesHistoryTracking.Models;

[Serializable]
public class SaleRecord {
    public ulong RetainerId { get; set; }
    public uint ItemId { get; set; }
    public uint Quantity { get; set; }
    public uint UnitPrice { get; set; }
    public string BuyerName { get; set; } = string.Empty;
    public DateTime SaleDate { get; set; }
}