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
        config.KnownListings ??= new List<TrackedListing>();

        return config.KnownListings
            .Where(listing => listing.AssociatedRetainerId == retainerId)
            .Select(listing => new ListingDisplayData {
                ItemId = listing.ItemId,
                Quantity = listing.Quantity,
                PricePerUnit = listing.PricePerUnit
            })
            .ToList();
    }

    private void RecordListings() {
        var activeRetainerId = this.listingProvider.GetActiveRetainerId();
        if (!activeRetainerId.HasValue) {
            this.logger.Warning("Could not identify the active retainer while trying to record listings.");
            return;
        }

        var fetchedListings = this.listingProvider.GetActiveRetainerListings();
        var config = this.configService.GetConfig();
        config.KnownListings ??= new List<TrackedListing>();

        // 1. Isolate existing listings to preserve prices if the UI parser returns 0
        var existingListings = config.KnownListings
            .Where(l => l.AssociatedRetainerId == activeRetainerId.Value)
            .ToList();

        // 2. Brutally remove ALL known listings for this retainer to purge any legacy duplicates
        config.KnownListings.RemoveAll(l => l.AssociatedRetainerId == activeRetainerId.Value);

        // 3. Merge fresh data and fallback to previous prices if current UI parsing failed
        foreach (var fetched in fetchedListings) {
            var existing = existingListings.FirstOrDefault(l => l.SlotIndex == fetched.SlotIndex)
                        ?? existingListings.FirstOrDefault(l => l.ItemId == fetched.ItemId);

            if (existing != null) {
                if (fetched.PricePerUnit == 0 && existing.PricePerUnit > 0) {
                    fetched.PricePerUnit = existing.PricePerUnit;
                }
            }

            config.KnownListings.Add(fetched);
        }

        this.configService.Save();
        this.logger.Info($"Successfully recorded {fetchedListings.Count} listings for retainer {activeRetainerId.Value}.");
    }

    public void Dispose() {
        this.gameEventService.RetainerListingsOpened -= this.RecordListings;
        this.gameEventService.RetainerListingAdded -= this.RecordListings;
    }
}