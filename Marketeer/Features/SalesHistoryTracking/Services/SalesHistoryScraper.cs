using Marketeer.Features.Logging.Contracts;
using Marketeer.Features.SalesHistoryTracking.Contracts;
using Marketeer.Features.SalesHistoryTracking.Models;
using Marketeer.Features.WindowAbstraction.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Marketeer.Features.SalesHistoryTracking.Services;

public class SalesHistoryScraper : ISalesHistoryScraper {
    private INativeWindowService windowService;
    private IItemResolverService itemResolver;
    private ILoggerService logger;

    public SalesHistoryScraper(
        INativeWindowService windowService,
        IItemResolverService itemResolver,
        ILoggerService logger) {
        this.windowService = windowService;
        this.itemResolver = itemResolver;
        this.logger = logger;
    }

    public bool IsHistoryWindowOpen() {
        var window = this.windowService.GetWindow("RetainerItemHistory");
        return window != null && window.IsVisible;
    }

    public IReadOnlyList<SaleRecord> ScrapeSales() {
        var records = new List<SaleRecord>();
        var window = this.windowService.GetWindow("RetainerItemHistory");

        if (window == null || !window.IsVisible) {
            this.logger.Warning("Cannot scrape sales: 'RetainerItemHistory' window is not visible.");
            return records;
        }

        var textElements = window.GetElements()
            .Where(e => e.Type == NativeUiElementType.Text)
            .Select(e => e.Text)
            .ToList();

        // Depending on FFXIV's exact node layout, we chunk the text elements.
        // Assuming a sequence: [ItemName + Qty], [UnitPrice], [BuyerName], [Date]
        int elementsPerRow = 4;

        for (int i = 0; i <= textElements.Count - elementsPerRow; i += elementsPerRow) {
            try {
                var rawNameQty = textElements[i];
                var rawPrice = textElements[i + 1];
                var buyerName = textElements[i + 2];
                var rawDate = textElements[i + 3];

                var (itemName, quantity) = this.ParseItemNameAndQuantity(rawNameQty);
                var itemId = this.itemResolver.ResolveItemId(itemName);

                // Clean formatting (e.g., thousands separators) before parsing
                var cleanPrice = rawPrice.Replace(",", "").Replace(" ", "");
                if (!uint.TryParse(cleanPrice, out var unitPrice)) {
                    continue;
                }

                if (!DateTime.TryParse(rawDate, out var saleDate)) {
                    // Fallback to current time if FFXIV specific date string parsing fails
                    saleDate = DateTime.UtcNow;
                }

                records.Add(new SaleRecord {
                    ItemId = itemId,
                    Quantity = quantity,
                    UnitPrice = unitPrice,
                    BuyerName = buyerName,
                    SaleDate = saleDate
                });
            }
            catch (Exception ex) {
                this.logger.Error(ex, $"Failed to parse sales history row at index {i}.");
            }
        }

        this.logger.Info($"Successfully scraped {records.Count} sale records from RetainerItemHistory.");
        return records;
    }

    private (string Name, uint Quantity) ParseItemNameAndQuantity(string rawText) {
        // FFXIV usually formats quantities as "ItemName x5" or uses the HQ symbol "".
        // We strip the HQ symbol and extract the quantity if it exists.
        var name = rawText.Replace("", "").Trim();
        uint quantity = 1;

        var match = Regex.Match(name, @"^(.*?)\s*x(\d+)$");
        if (match.Success) {
            name = match.Groups[1].Value.Trim();
            uint.TryParse(match.Groups[2].Value, out quantity);
        }

        return (name, quantity);
    }
}