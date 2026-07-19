using Marketeer.Features.SalesHistoryUI.Models;
using System.Collections.Generic;

namespace Marketeer.Features.SalesHistoryUI.Contracts;

public interface ISalesDataPresenter {
    IReadOnlyList<ISalesViewRecord> ProcessData(IReadOnlyList<ISalesViewRecord> rawData, string searchQuery, SalesSortColumn sortColumn, bool isAscending);
}