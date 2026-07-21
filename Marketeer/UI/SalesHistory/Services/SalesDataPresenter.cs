using Marketeer.API.SalesHistory.Contracts;
using Marketeer.API.SalesHistory.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Marketeer.UI.SalesHistory.Services;

public class SalesDataPresenter : ISalesDataPresenter {
    public IReadOnlyList<ISalesViewRecord> ProcessData(IReadOnlyList<ISalesViewRecord> rawData, string searchQuery, SalesSortColumn sortColumn, bool isAscending) {
        var filteredData = string.IsNullOrWhiteSpace(searchQuery)
            ? rawData
            : rawData.Where(x => x.Name.Contains(searchQuery, StringComparison.OrdinalIgnoreCase));

        return sortColumn switch {
            SalesSortColumn.Name => isAscending ? filteredData.OrderBy(x => x.Name).ToList() : filteredData.OrderByDescending(x => x.Name).ToList(),
            SalesSortColumn.Quantity => isAscending ? filteredData.OrderBy(x => x.TotalQuantitySold).ToList() : filteredData.OrderByDescending(x => x.TotalQuantitySold).ToList(),
            SalesSortColumn.AveragePrice => isAscending ? filteredData.OrderBy(x => x.AverageUnitPrice).ToList() : filteredData.OrderByDescending(x => x.AverageUnitPrice).ToList(),
            SalesSortColumn.TotalRevenue => isAscending ? filteredData.OrderBy(x => x.TotalRevenue).ToList() : filteredData.OrderByDescending(x => x.TotalRevenue).ToList(),
            SalesSortColumn.LastSaleDate => isAscending ? filteredData.OrderBy(x => x.LastSaleDate).ToList() : filteredData.OrderByDescending(x => x.LastSaleDate).ToList(),
            _ => filteredData.ToList()
        };
    }
}