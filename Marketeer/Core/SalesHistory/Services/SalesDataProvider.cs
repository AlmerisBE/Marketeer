using Marketeer.API.SalesHistory.Contracts;
using Marketeer.API.SalesHistory.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Marketeer.Core.SalesHistory.Services;

public class SalesDataProvider : ISalesDataProvider {
    private ISalesRepository salesRepository;
    private ISalesAnalysisService analysisService;
    private IItemResolverService itemResolver;

    public SalesDataProvider(
        ISalesRepository salesRepository,
        ISalesAnalysisService analysisService,
        IItemResolverService itemResolver) {
        this.salesRepository = salesRepository;
        this.analysisService = analysisService;
        this.itemResolver = itemResolver;
    }

    public IReadOnlyList<ISalesViewRecord> GetSalesData(SalesPeriod period) {
        var rawSales = this.salesRepository.GetAllSales();
        var filteredSales = this.FilterByPeriod(rawSales, period);
        var groupedSales = filteredSales.GroupBy(s => s.ItemId);
        var viewRecords = new List<ISalesViewRecord>();

        foreach (var group in groupedSales) {
            var itemId = group.Key;
            var itemSales = group.ToList();
            var summary = this.analysisService.AnalyzeItem(itemId, itemSales);

            viewRecords.Add(new SalesViewRecord {
                ItemId = itemId,
                IconId = this.itemResolver.ResolveIconId(itemId),
                Name = this.itemResolver.ResolveItemName(itemId),
                TotalQuantitySold = summary.TotalQuantitySold,
                AverageUnitPrice = summary.AverageUnitPrice,
                TotalRevenue = summary.TotalRevenue,
                LastSaleDate = itemSales.Max(s => s.SaleDate)
            });
        }

        return viewRecords;
    }

    private IEnumerable<SaleRecord> FilterByPeriod(IEnumerable<SaleRecord> sales, SalesPeriod period) {
        var today = DateTime.UtcNow.Date;

        return period switch {
            SalesPeriod.Today => sales.Where(s => s.SaleDate.Date == today),
            SalesPeriod.Yesterday => sales.Where(s => s.SaleDate.Date == today.AddDays(-1)),
            SalesPeriod.ThisWeek => sales.Where(s => s.SaleDate.Date >= today.AddDays(-(int)today.DayOfWeek)),
            SalesPeriod.ThisMonth => sales.Where(s => s.SaleDate.Month == today.Month && s.SaleDate.Year == today.Year),
            SalesPeriod.LastMonth => sales.Where(s => s.SaleDate.Month == today.AddMonths(-1).Month && s.SaleDate.Year == today.AddMonths(-1).Year),
            SalesPeriod.ThisQuarter => sales.Where(s => s.SaleDate.Year == today.Year && (s.SaleDate.Month - 1) / 3 == (today.Month - 1) / 3),
            SalesPeriod.ThisYear => sales.Where(s => s.SaleDate.Year == today.Year),
            _ => sales
        };
    }
}