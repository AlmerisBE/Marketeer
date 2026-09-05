using Marketeer.Core.SalesHistory.Models;
using System.Collections.Generic;

namespace Marketeer.Core.SalesHistory.Contracts;

public interface ISalesStatisticsService {
    SalesGlobalSummary GetGlobalSummary();
    IReadOnlyList<ItemSalesSummary> GetTopBestSellers(int limit = 10);
    IReadOnlyList<FastestSellingItem> GetFastestSellingItems(int limit = 10);
    IReadOnlyList<DailyChartData> GetDailySalesChartData(int days = 14);
}