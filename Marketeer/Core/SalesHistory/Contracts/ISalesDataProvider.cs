using Marketeer.Core.SalesHistory.Models;
using System.Collections.Generic;

namespace Marketeer.Core.SalesHistory.Contracts;

public interface ISalesDataProvider {
    IReadOnlyList<ISalesViewRecord> GetSalesData(SalesPeriod period);
}