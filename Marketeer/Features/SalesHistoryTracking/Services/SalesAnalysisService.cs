using Marketeer.Features.SalesHistoryTracking.Contracts;
using Marketeer.Features.SalesHistoryTracking.Models;
using System.Collections.Generic;
using System.Linq;

namespace Marketeer.Features.SalesHistoryTracking.Services;

public class SalesAnalysisService : ISalesAnalysisService {

    public ItemSalesSummary AnalyzeItem(uint itemId, IReadOnlyList<SaleRecord> salesHistory) {
        var summary = new ItemSalesSummary {
            ItemId = itemId
        };

        if (salesHistory == null || salesHistory.Count == 0) {
            return summary;
        }

        var itemSales = salesHistory.Where(s => s.ItemId == itemId).ToList();

        if (itemSales.Count == 0) {
            return summary;
        }

        // Somme des quantités : on force int pour lever l'ambiguïté puis on convertit en uint.
        summary.TotalQuantitySold = (uint)itemSales.Sum(s => (int)s.Quantity);

        // TotalRevenue : on utilise Aggregate pour accumuler en ulong (pas de Sum pour ulong).
        summary.TotalRevenue = itemSales.Aggregate(0UL, (acc, s) => acc + (ulong)s.Quantity * s.UnitPrice);

        summary.AverageStackSize = (double)summary.TotalQuantitySold / itemSales.Count;
        summary.AverageUnitPrice = (double)summary.TotalRevenue / summary.TotalQuantitySold;

        var minDate = itemSales.Min(s => s.SaleDate);
        var maxDate = itemSales.Max(s => s.SaleDate);
        var timespan = maxDate - minDate;

        // Prevent division by zero if all sales happened at the exact same second, or if there is only 1 sale
        var daysElapsed = timespan.TotalDays > 0 ? timespan.TotalDays : 1.0;
        summary.SalesPerDay = summary.TotalQuantitySold / daysElapsed;

        return summary;
    }
}