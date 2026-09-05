using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;
using Marketeer.API.GameInterop.Contracts;
using Marketeer.Core.Configuration.Contracts;
using Marketeer.Core.Financials.Models;
using Marketeer.Core.Logging.Contracts;
using Marketeer.Core.MarketListings.Contracts;
using Marketeer.Core.MarketListings.Models;
using Marketeer.Core.SalesHistory.Contracts;
using Marketeer.Core.SalesHistory.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Marketeer.Core.MarketListings.Services;

public class MarketListingTrackerService : IMarketListingTrackerService, IDisposable {

    public event Action<uint>? LocalListingModified;

    private IConfigurationService configService;
    private IGameEventService gameEventService;
    private IMarketListingProvider listingProvider;
    private IDataManager dataManager;
    private ISalesInferenceService inferenceService;
    private ILoggerService logger;

    private bool isManualFirstScan = true;

    public MarketListingTrackerService(
        IConfigurationService configService,
        IGameEventService gameEventService,
        IMarketListingProvider listingProvider,
        IDataManager dataManager,
        ISalesInferenceService inferenceService,
        ILoggerService logger) {

        this.configService = configService;
        this.gameEventService = gameEventService;
        this.listingProvider = listingProvider;
        this.dataManager = dataManager;
        this.inferenceService = inferenceService;
        this.logger = logger;

        this.gameEventService.RetainerBellOpened += this.OnRetainerBellOpened;
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

    public bool ScanListings(ulong retainerId, bool isFirstScan) {
        var activeRetainerIdOpt = this.listingProvider.GetActiveRetainerId();

        if (!activeRetainerIdOpt.HasValue || activeRetainerIdOpt.Value != retainerId) {
            this.logger.Warning($"[MarketListingTrackerService] Attempted to scan retainer {retainerId}, but active retainer is {(activeRetainerIdOpt.HasValue ? activeRetainerIdOpt.Value.ToString() : "none")}.");
            return false;
        }

        var fetchedListings = this.listingProvider.GetActiveRetainerListings();
        var config = this.configService.GetConfig();

        lock (config) {
            RetainerFinancialData? targetRetainer = null;
            foreach (var charData in config.FinancialRecords.Values) {
                if (charData.Retainers.TryGetValue(retainerId, out var r)) {
                    targetRetainer = r;
                    break;
                }
            }

            if (targetRetainer == null) {
                this.logger.Warning($"[MarketListingTrackerService] Retainer {retainerId} not found in configuration.");
                return false;
            }

            var previousListings = targetRetainer.MarketListings.Select(kvp => new ListingState {
                SlotIndex = kvp.Key,
                ItemId = kvp.Value.ItemId,
                Quantity = kvp.Value.Quantity,
                UnitPrice = kvp.Value.PricePerUnit
            }).ToList();

            var currentListings = fetchedListings.Select(f => new ListingState {
                SlotIndex = (int)f.SlotIndex,
                ItemId = f.ItemId,
                Quantity = f.Quantity,
                UnitPrice = f.PricePerUnit
            }).ToList();

            this.inferenceService.InferSales(retainerId, isFirstScan, previousListings, currentListings);

            var oldListings = targetRetainer.MarketListings.ToDictionary(k => k.Key, v => v.Value);
            targetRetainer.MarketListings.Clear();

            bool isModified = oldListings.Count != fetchedListings.Count;
            var modifiedItems = new HashSet<uint>();

            // Identify items that were removed (sold or manually cancelled)
            var fetchedItemIds = new HashSet<uint>(fetchedListings.Select(f => f.ItemId));
            foreach (var old in oldListings.Values) {
                if (!fetchedItemIds.Contains(old.ItemId)) {
                    modifiedItems.Add(old.ItemId);
                }
            }

            foreach (var fetched in fetchedListings) {
                uint finalPrice = fetched.PricePerUnit;
                DateTime listingDate = DateTime.UtcNow;

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

                    if (existing == null || existing.ItemId != fetched.ItemId || existing.PricePerUnit != finalPrice) {
                        modifiedItems.Add(fetched.ItemId);
                    }
                }
                else {
                    listingDate = existing.ListingDate;
                }

                targetRetainer.MarketListings[(int)fetched.SlotIndex] = new RetainerMarketListingSaveData {
                    ItemId = fetched.ItemId,
                    Quantity = fetched.Quantity,
                    PricePerUnit = finalPrice,
                    ListingDate = listingDate
                };
            }

            if (isModified || isFirstScan) {
                this.configService.Save();
                this.logger.Debug($"[MarketListingTrackerService] State modified. Saved {fetchedListings.Count} listings for retainer {retainerId}.");

                foreach (var itemId in modifiedItems) {
                    this.LocalListingModified?.Invoke(itemId);
                }
            }
        }

        return true;
    }

    private void OnRetainerBellOpened() {
        this.isManualFirstScan = true;
    }

    private void RecordListings() {
        var activeRetainerIdOpt = this.listingProvider.GetActiveRetainerId();
        if (!activeRetainerIdOpt.HasValue) {
            return;
        }

        this.ScanListings(activeRetainerIdOpt.Value, this.isManualFirstScan);
        this.isManualFirstScan = false;
    }

    private string GetItemName(uint itemId) {
        var sheet = this.dataManager.GetExcelSheet<Item>();
        uint baseItemId = itemId > 1000000u ? itemId - 1000000u : itemId;

        if (sheet != null && sheet.HasRow(baseItemId)) {
            return sheet.GetRow(baseItemId).Name.ToString();
        }

        return "Unknown Item";
    }

    public void Dispose() {
        this.gameEventService.RetainerBellOpened -= this.OnRetainerBellOpened;
        this.gameEventService.RetainerSellListUpdated -= this.RecordListings;
    }
}