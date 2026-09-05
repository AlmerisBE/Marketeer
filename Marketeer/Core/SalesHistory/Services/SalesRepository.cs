using Marketeer.Core.Configuration.Contracts;
using Marketeer.Core.SalesHistory.Contracts;
using Marketeer.Core.SalesHistory.Models;
using System.Collections.Generic;
using System.Linq;

namespace Marketeer.Core.SalesHistory.Services;

public class SalesRepository : ISalesRepository {
    private IConfigurationService configService;
    private HashSet<SaleRecord> sales;

    public SalesRepository(IConfigurationService configService) {
        this.configService = configService;

        var existingSales = this.configService.GetConfig().SalesHistory ?? new List<SaleRecord>();
        this.sales = new HashSet<SaleRecord>(existingSales);
    }

    public void AddSales(IEnumerable<SaleRecord> newSales) {
        bool isModified = false;

        foreach (var sale in newSales) {
            if (this.sales.Add(sale)) {
                isModified = true;
            }
        }

        if (isModified) {
            this.configService.GetConfig().SalesHistory = this.sales.ToList();
            this.configService.Save();
        }
    }

    public IReadOnlyList<SaleRecord> GetAllSales() {
        return this.sales.ToList().AsReadOnly();
    }

    public void ClearSales() {
        if (this.sales.Count > 0) {
            this.sales.Clear();
            this.configService.GetConfig().SalesHistory.Clear();
            this.configService.Save();
        }
    }
}