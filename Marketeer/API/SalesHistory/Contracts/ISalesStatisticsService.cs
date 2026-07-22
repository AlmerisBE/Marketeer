using Marketeer.API.SalesHistory.Models;
using System.Collections.Generic;

namespace Marketeer.API.SalesHistory.Contracts;

public interface ISalesStatisticsService {
    SalesGlobalSummary GetGlobalSummary();
    IReadOnlyList<ItemSalesSummary> GetTopBestSellers(int limit = 10);
    IReadOnlyList<FastestSellingItem> GetFastestSellingItems(int limit = 10);
    IReadOnlyList<DailyChartData> GetDailySalesChartData(int days = 14);
}