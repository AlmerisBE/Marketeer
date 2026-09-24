using Marketeer.Core.SalesHistory.Models;
using System;
using System.Collections.Generic;

namespace Marketeer.Core.SalesHistory.Contracts;

public interface ISalesRepository {
    void AddSales(IEnumerable<SaleRecord> sales);
    IReadOnlyList<SaleRecord> GetAllSales();
    void ClearSales();

    SalesGlobalSummary GetGlobalSummary();
    IReadOnlyList<ItemSalesSummary> GetTopBestSellers(int limit);
    IReadOnlyList<FastestSellingItem> GetFastestSellingItems(int limit);
    IReadOnlyList<SaleRecord> GetSalesSince(DateTime cutoff);
}