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

        this.gameEventService.RetainerListingsOpened += this.RecordListings;
        this.gameEventService.RetainerListingAdded += this.RecordListings;
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
        var activeRetainerId = this.listingProvider.GetActiveRetainerId();
        if (!activeRetainerId.HasValue) {
            return;
        }

        var fetchedListings = this.listingProvider.GetActiveRetainerListings();
        var config = this.configService.GetConfig();

        lock (config) {
            config.KnownListings ??= new List<TrackedListing>();

            var existingListings = config.KnownListings
                .Where(l => l.AssociatedRetainerId == activeRetainerId.Value)
                .ToList();

            bool isModified = false;

            if (existingListings.Count != fetchedListings.Count) {
                isModified = true;
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
                    }

                    if (existing.PricePerUnit != fetched.PricePerUnit || existing.Quantity != fetched.Quantity || existing.ItemId != fetched.ItemId) {
                        isModified = true;
                    }
                }
                else {
                    isModified = true;
                }

                config.KnownListings.Add(fetched);
            }

            if (isModified) {
                this.configService.Save();
                this.logger.Info($"[MarketListing] State modified. Saved {fetchedListings.Count} listings for retainer {activeRetainerId.Value}.");
            }
        }
    }

    public void Dispose() {
        this.gameEventService.RetainerListingsOpened -= this.RecordListings;
        this.gameEventService.RetainerListingAdded -= this.RecordListings;
    }
}