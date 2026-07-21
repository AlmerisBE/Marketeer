using Marketeer.API.SalesHistory.Models;
using System.Collections.Generic;

namespace Marketeer.API.SalesHistory.Contracts;

public interface ISalesRepository {
    void AddSales(IEnumerable<SaleRecord> sales);
    IReadOnlyList<SaleRecord> GetAllSales();
    void ClearSales();
}