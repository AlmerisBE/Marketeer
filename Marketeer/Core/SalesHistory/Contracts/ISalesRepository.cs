using Marketeer.Core.SalesHistory.Models;
using System.Collections.Generic;

namespace Marketeer.Core.SalesHistory.Contracts;

public interface ISalesRepository {
    void AddSales(IEnumerable<SaleRecord> sales);
    IReadOnlyList<SaleRecord> GetAllSales();
    void ClearSales();
}