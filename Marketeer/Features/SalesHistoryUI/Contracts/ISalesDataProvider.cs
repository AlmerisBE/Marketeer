using System.Collections.Generic;

namespace Marketeer.Features.SalesHistoryUI.Contracts;

public interface ISalesDataProvider {
    IReadOnlyList<ISalesViewRecord> GetSalesData();
}