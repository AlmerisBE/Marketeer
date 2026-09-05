using Marketeer.Core.SalesHistory.Models;
using System.Collections.Generic;

namespace Marketeer.Core.SalesHistory.Contracts;

public interface ISalesHistoryScraper {
    bool IsHistoryWindowOpen();
    IReadOnlyList<SaleRecord> ScrapeSales();
}