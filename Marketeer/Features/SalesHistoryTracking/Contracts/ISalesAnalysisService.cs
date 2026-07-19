using Marketeer.Features.SalesHistoryTracking.Models;
using System.Collections.Generic;

namespace Marketeer.Features.SalesHistoryTracking.Contracts;

public interface ISalesAnalysisService {
    ItemSalesSummary AnalyzeItem(uint itemId, IReadOnlyList<SaleRecord> salesHistory);
}