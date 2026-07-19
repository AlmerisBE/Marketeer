using Marketeer.Features.SalesHistoryTracking.Models;
using System.Collections.Generic;

namespace Marketeer.Features.SalesHistoryTracking.Contracts;

public interface ISalesHistoryScraper {
    bool IsHistoryWindowOpen();
    IReadOnlyList<SaleRecord> ScrapeSales();
}