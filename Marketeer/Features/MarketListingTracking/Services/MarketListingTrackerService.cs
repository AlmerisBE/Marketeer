using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;
using Marketeer.Features.Configuration.Contracts;
using Marketeer.Features.Dashboard.Contracts;
using Marketeer.Features.Dashboard.Models;
using Marketeer.Features.Financials.Models;
using Marketeer.Features.GameEvents.Contracts;
using Marketeer.Features.Logging.Contracts;
using Marketeer.Features.MarketListingTracking.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Marketeer.Features.MarketListingTracking.Services;

public class MarketListingTrackerService : IMarketListingTrackerService, IDisposable {
    private IConfigurationService configService;
    private IGameEventService gameEventService;
    private IMarketListingProvider listingProvider;
    private IDataManager dataManager;
    private ILoggerService logger;

    public MarketListingTrackerService(
        IConfigurationService configService,
        IGameEventService gameEventService,
        IMarketListingProvider listingProvider,
        IDataManager dataManager,
        ILoggerService logger) {

        this.configService = configService;
        this.gameEventService = gameEventService;
        this.listingProvider = listingProvider;
        this.dataManager = dataManager;
        this.logger = logger;

        this.gameEventService.RetainerSellListUpdated += this.RecordListings;
    }

    public IReadOnlyList<ListingDisplayData> GetListingsForRetainer(ulong retainerId) {
        var config = this.configService.GetConfig();

        lock (config) {
            foreach (var charData in config.FinancialRecords.Values) {
                if (charData.Retainers.TryGetValue(retainerId, out var rData)) {
                    return rData.MarketListings.Values.Select(l => new ListingDisplayData {
                        ItemId = l.ItemId,
                        ItemName = this.GetItemName(l.ItemId),
                        Quantity = l.Quantity,
                        PricePerUnit = l.PricePerUnit,
                        TotalPrice = l.PricePerUnit * l.Quantity,
                        Tax = (uint)Math.Floor((l.PricePerUnit * l.Quantity) * 0.05)
                    }).ToList();
                }
            }
            return new List<ListingDisplayData>();
        }
    }

    private string GetItemName(uint itemId) {
        var sheet = this.dataManager.GetExcelSheet<Item>();
        uint baseItemId = itemId > 1000000u ? itemId - 1000000u : itemId;

        if (sheet != null && sheet.HasRow(baseItemId)) {
            return sheet.GetRow(baseItemId).Name.ToString();
        }
        return "Unknown Item";
    }

    private void RecordListings() {
        var activeRetainerIdOpt = this.listingProvider.GetActiveRetainerId();
        if (!activeRetainerIdOpt.HasValue) {
            return;
        }

        var fetchedListings = this.listingProvider.GetActiveRetainerListings();
        if (fetchedListings.Count == 0) {
            return;
        }

        var config = this.configService.GetConfig();

        lock (config) {
            RetainerFinancialData? targetRetainer = null;
            foreach (var charData in config.FinancialRecords.Values) {
                if (charData.Retainers.TryGetValue(activeRetainerIdOpt.Value, out var r)) {
                    targetRetainer = r;
                    break;
                }
            }

            if (targetRetainer == null) {
                return;
            }

            var oldListings = targetRetainer.MarketListings.ToDictionary(k => k.Key, v => v.Value);
            targetRetainer.MarketListings.Clear();
            bool isModified = oldListings.Count != fetchedListings.Count;

            foreach (var fetched in fetchedListings) {
                uint finalPrice = fetched.PricePerUnit;

                // Fallback mechanism to protect against 0-gil UI network delays
                if (finalPrice == 0 && oldListings.TryGetValue((int)fetched.SlotIndex, out var oldListing)) {
                    if (oldListing.ItemId == fetched.ItemId && oldListing.PricePerUnit > 0) {
                        finalPrice = oldListing.PricePerUnit;
                    }
                }

                if (!oldListings.TryGetValue((int)fetched.SlotIndex, out var existing) ||
                    existing.ItemId != fetched.ItemId ||
                    existing.Quantity != fetched.Quantity ||
                    existing.PricePerUnit != finalPrice) {
                    isModified = true;
                }

                targetRetainer.MarketListings[(int)fetched.SlotIndex] = new RetainerMarketListingSaveData {
                    ItemId = fetched.ItemId,
                    Quantity = fetched.Quantity,
                    PricePerUnit = finalPrice
                };
            }

            if (isModified) {
                this.configService.Save();
                this.logger.Info($"[MarketListingTrackerService] State modified. Saved {fetchedListings.Count} listings for retainer {activeRetainerIdOpt.Value}.");
            }
        }
    }

    public void Dispose() {
        this.gameEventService.RetainerSellListUpdated -= this.RecordListings;
    }
}