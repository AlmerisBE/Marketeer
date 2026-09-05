using Marketeer.Core.SalesHistory.Models;
using System.Collections.Generic;

namespace Marketeer.Core.SalesHistory.Contracts;

public interface ISalesAnalysisService {
    ItemSalesSummary AnalyzeItem(uint itemId, IReadOnlyList<SaleRecord> salesHistory);
}