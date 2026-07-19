using Marketeer.Features.SalesHistoryTracking.Contracts;
using Marketeer.Features.SalesHistoryTracking.Models;
using Marketeer.Features.SalesHistoryUI.Contracts;
using System.Collections.Generic;
using System.Linq;

namespace Marketeer.Features.SalesHistoryTracking.Services;

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

    public IReadOnlyList<ISalesViewRecord> GetSalesData() {
        var rawSales = this.salesRepository.GetAllSales();
        var groupedSales = rawSales.GroupBy(s => s.ItemId);
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
}