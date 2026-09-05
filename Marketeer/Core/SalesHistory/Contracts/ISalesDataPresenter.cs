using Marketeer.Core.SalesHistory.Models;
using System.Collections.Generic;

namespace Marketeer.Core.SalesHistory.Contracts;

public interface ISalesDataPresenter {
    IReadOnlyList<ISalesViewRecord> ProcessData(IReadOnlyList<ISalesViewRecord> rawData, string searchQuery, SalesSortColumn sortColumn, bool isAscending);
}