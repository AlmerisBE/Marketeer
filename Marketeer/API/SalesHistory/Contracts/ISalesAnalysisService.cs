using Marketeer.API.SalesHistory.Models;
using System.Collections.Generic;

namespace Marketeer.API.SalesHistory.Contracts;

public interface ISalesAnalysisService {
    ItemSalesSummary AnalyzeItem(uint itemId, IReadOnlyList<SaleRecord> salesHistory);
}