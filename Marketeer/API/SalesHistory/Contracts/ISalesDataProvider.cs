using Marketeer.API.SalesHistory.Models;
using System.Collections.Generic;

namespace Marketeer.API.SalesHistory.Contracts;

public interface ISalesDataProvider {
    IReadOnlyList<ISalesViewRecord> GetSalesData();
}