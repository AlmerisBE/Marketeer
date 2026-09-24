using Marketeer.Core.SalesHistory.Contracts;
using Marketeer.Core.SalesHistory.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Marketeer.Core.SalesHistory.Services;

public class SalesStatisticsService : ISalesStatisticsService {
    private ISalesRepository salesRepository;
    private IItemResolverService itemResolver;

    public SalesStatisticsService(ISalesRepository salesRepository, IItemResolverService itemResolver) {
        this.salesRepository = salesRepository;
        this.itemResolver = itemResolver;
    }

    public SalesGlobalSummary GetGlobalSummary() {
        return this.salesRepository.GetGlobalSummary();
    }

    public IReadOnlyList<ItemSalesSummary> GetTopBestSellers(int limit = 10) {
        return this.salesRepository.GetTopBestSellers(limit);
    }

    public IReadOnlyList<FastestSellingItem> GetFastestSellingItems(int limit = 10) {
        var items = this.salesRepository.GetFastestSellingItems(limit);

        foreach (var item in items) {
            item.ItemName = this.itemResolver.ResolveItemName(item.ItemId);
        }

        return items;
    }

    public IReadOnlyList<DailyChartData> GetDailySalesChartData(int days = 14) {
        var cutoff = DateTime.UtcNow.Date.AddDays(-days + 1);
        var recentSales = this.salesRepository.GetSalesSince(cutoff);

        var grouped = recentSales.GroupBy(s => s.SaleDate.Date).ToDictionary(g => g.Key, g => g.ToList());
        var results = new List<DailyChartData>();

        for (int i = 0; i < days; i++) {
            var date = cutoff.AddDays(i);
            if (grouped.TryGetValue(date, out var daySales)) {
                results.Add(new DailyChartData {
                    Date = date,
                    SalesCount = daySales.Count,
                    Revenue = daySales.Aggregate(0ul, (acc, s) => acc + (ulong)s.Quantity * s.UnitPrice)
                });
            }
            else results.Add(new DailyChartData { Date = date, SalesCount = 0, Revenue = 0 });
        }

        return results;
    }
}