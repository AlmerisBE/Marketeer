using Marketeer.Features.SalesHistoryTracking.Contracts;
using Marketeer.Features.SalesHistoryTracking.Models;
using System.Collections.Generic;

namespace Marketeer.Features.SalesHistoryTracking.Services;

public class SalesRepository : ISalesRepository {
    private List<SaleRecord> sales = [];

    public void AddSales(IEnumerable<SaleRecord> newSales) {
        this.sales.AddRange(newSales);
    }

    public IReadOnlyList<SaleRecord> GetAllSales() {
        return this.sales.AsReadOnly();
    }
}