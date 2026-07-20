using Marketeer.Features.SalesHistoryTracking.Models;
using System.Collections.Generic;

namespace Marketeer.Features.SalesHistoryTracking.Contracts;

public interface ISalesRepository {
    void AddSales(IEnumerable<SaleRecord> sales);
    IReadOnlyList<SaleRecord> GetAllSales();
    void ClearSales();
}