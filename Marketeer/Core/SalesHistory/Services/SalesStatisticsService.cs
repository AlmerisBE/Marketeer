using Marketeer.API.SalesHistory.Contracts;
using Marketeer.API.SalesHistory.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Marketeer.Core.SalesHistory.Services;

public class SalesStatisticsService : ISalesStatisticsService {
    private ISalesRepository salesRepository;
    private IItemResolverService itemResolver;
    private ISalesAnalysisService analysisService;

    public SalesStatisticsService(ISalesRepository salesRepository, IItemResolverService itemResolver, ISalesAnalysisService analysisService) {
        this.salesRepository = salesRepository;
        this.itemResolver = itemResolver;
        this.analysisService = analysisService;
    }

    public SalesGlobalSummary GetGlobalSummary() {
        var sales = this.salesRepository.GetAllSales();
        if (sales.Count == 0) {
            return new SalesGlobalSummary();
        }

        var totalItems = (uint)sales.Sum(s => s.Quantity);
        var totalRevenue = sales.Aggregate(0ul, (acc, s) => acc + (ulong)s.Quantity * s.UnitPrice);

        return new SalesGlobalSummary {
            TotalSalesCount = sales.Count,
            TotalItemsSold = totalItems,
            AverageItemsPerSale = (double)totalItems / sales.Count,
            TotalRevenue = totalRevenue,
            AverageRevenuePerSale = (double)totalRevenue / sales.Count
        };
    }

    public IReadOnlyList<ItemSalesSummary> GetTopBestSellers(int limit = 10) {
        var sales = this.salesRepository.GetAllSales();
        var grouped = sales.GroupBy(s => s.ItemId);
        var summaries = new List<ItemSalesSummary>();

        foreach (var group in grouped) {
            summaries.Add(this.analysisService.AnalyzeItem(group.Key, group.ToList()));
        }

        return summaries.OrderByDescending(s => s.TotalRevenue).Take(limit).ToList();
    }

    public IReadOnlyList<FastestSellingItem> GetFastestSellingItems(int limit = 10) {
        var sales = this.salesRepository.GetAllSales()
            .Where(s => s.ListingDate != DateTime.MinValue && s.SaleDate > s.ListingDate)
            .ToList();

        var grouped = sales.GroupBy(s => s.ItemId);
        var results = new List<FastestSellingItem>();

        foreach (var group in grouped) {
            var avgTicks = group.Average(s => (s.SaleDate - s.ListingDate).Ticks);
            results.Add(new FastestSellingItem {
                ItemId = group.Key,
                ItemName = this.itemResolver.ResolveItemName(group.Key),
                AverageTimeToSell = TimeSpan.FromTicks((long)avgTicks),
                SalesCount = group.Count()
            });
        }

        return results.OrderBy(x => x.AverageTimeToSell).Take(limit).ToList();
    }

    public IReadOnlyList<DailyChartData> GetDailySalesChartData(int days = 14) {
        var sales = this.salesRepository.GetAllSales();
        var cutoff = DateTime.UtcNow.Date.AddDays(-days + 1);
        var recentSales = sales.Where(s => s.SaleDate.Date >= cutoff).ToList();

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
            else {
                results.Add(new DailyChartData { Date = date, SalesCount = 0, Revenue = 0 });
            }
        }

        return results;
    }
}