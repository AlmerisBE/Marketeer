using Marketeer.Features.Configuration.Contracts;
using Marketeer.Features.Dashboard.Contracts;
using Marketeer.Features.Dashboard.Models;
using Marketeer.Features.GameEvents.Contracts;
using Marketeer.Features.Logging.Contracts;
using Marketeer.Features.MarketListingTracking.Contracts;
using Marketeer.Features.MarketListingTracking.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Marketeer.Features.MarketListingTracking.Services;

public class MarketListingTrackerService : IMarketListingTrackerService, IDisposable {
    private IConfigurationService configService;
    private IGameEventService gameEventService;
    private IMarketListingProvider listingProvider;
    private ILoggerService logger;

    public MarketListingTrackerService(
        IConfigurationService configService,
        IGameEventService gameEventService,
        IMarketListingProvider listingProvider,
        ILoggerService logger) {

        this.configService = configService;
        this.gameEventService = gameEventService;
        this.listingProvider = listingProvider;
        this.logger = logger;

        this.gameEventService.RetainerSellListUpdated += this.RecordListings;
    }

    public IReadOnlyList<ListingDisplayData> GetListingsForRetainer(ulong retainerId) {
        var config = this.configService.GetConfig();

        lock (config) {
            config.KnownListings ??= new List<TrackedListing>();

            return config.KnownListings
                .Where(listing => listing.AssociatedRetainerId == retainerId)
                .Select(listing => new ListingDisplayData {
                    ItemId = listing.ItemId,
                    ItemName = listing.ItemName,
                    Quantity = listing.Quantity,
                    PricePerUnit = listing.PricePerUnit,
                    TotalPrice = listing.TotalPrice,
                    Tax = listing.Tax
                })
                .ToList();
        }
    }

    private void RecordListings() {
        this.logger.Debug("[MarketListingTrackerService] RecordListings triggered by GameEventService.");

        var activeRetainerId = this.listingProvider.GetActiveRetainerId();
        if (!activeRetainerId.HasValue) {
            return;
        }

        var fetchedListings = this.listingProvider.GetActiveRetainerListings();
        this.logger.Debug($"[MarketListingTrackerService] Fetched {fetchedListings.Count} listings from provider.");

        if (fetchedListings.Count == 0) {
            return;
        }

        var config = this.configService.GetConfig();

        lock (config) {
            config.KnownListings ??= new List<TrackedListing>();

            var existingListings = config.KnownListings
                .Where(l => l.AssociatedRetainerId == activeRetainerId.Value)
                .ToList();

            bool isModified = false;

            if (existingListings.Count != fetchedListings.Count) {
                isModified = true;
                this.logger.Debug($"[MarketListingTrackerService] Listing count changed from {existingListings.Count} to {fetchedListings.Count}. Marking as modified.");
            }

            config.KnownListings.RemoveAll(l => l.AssociatedRetainerId == activeRetainerId.Value);

            foreach (var fetched in fetchedListings) {
                var existing = existingListings.FirstOrDefault(l => l.SlotIndex == fetched.SlotIndex)
                            ?? existingListings.FirstOrDefault(l => l.ItemId == fetched.ItemId);

                if (existing != null) {
                    if (fetched.PricePerUnit == 0 && existing.PricePerUnit > 0) {
                        fetched.PricePerUnit = existing.PricePerUnit;
                        fetched.TotalPrice = fetched.PricePerUnit * fetched.Quantity;
                        fetched.Tax = (uint)Math.Floor(fetched.TotalPrice * 0.05);
                        this.logger.Debug($"[MarketListingTrackerService] Restored missing price {fetched.PricePerUnit} for item {fetched.ItemName}.");
                    }

                    if (existing.PricePerUnit != fetched.PricePerUnit || existing.Quantity != fetched.Quantity || existing.ItemId != fetched.ItemId) {
                        isModified = true;
                        this.logger.Debug($"[MarketListingTrackerService] Value modified for {fetched.ItemName}: Price {existing.PricePerUnit} -> {fetched.PricePerUnit}, Qty {existing.Quantity} -> {fetched.Quantity}.");
                    }
                }
                else {
                    isModified = true;
                    this.logger.Debug($"[MarketListingTrackerService] New listing discovered for {fetched.ItemName}. Marking as modified.");
                }

                config.KnownListings.Add(fetched);
            }

            if (isModified) {
                this.configService.Save();
                this.logger.Info($"[MarketListingTrackerService] State modified. Saved {fetchedListings.Count} listings for retainer {activeRetainerId.Value}.");
            }
            else {
                this.logger.Debug("[MarketListingTrackerService] No state changes detected. Save bypassed.");
            }
        }
    }

    public void Dispose() {
        this.gameEventService.RetainerSellListUpdated -= this.RecordListings;
    }
}