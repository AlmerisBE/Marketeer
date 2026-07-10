using Marketeer.Features.CharacterTracking.Contracts;
using Marketeer.Features.CharacterTracking.Models;
using Marketeer.Features.Configuration.Contracts;
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

        // Merge logic: Retain existing prices if the UI parser failed to grab them this time
        foreach (var fetched in fetchedListings) {
            var existing = config.KnownListings.FirstOrDefault(l => l.AssociatedRetainerId == activeRetainerId.Value && l.ItemId == fetched.ItemId);
            if (existing != null) {
                existing.Quantity = fetched.Quantity;
                if (fetched.PricePerUnit > 0) {
                    existing.PricePerUnit = fetched.PricePerUnit;
                }
            }
            else {
                config.KnownListings.Add(fetched);
            }
        }

        // Remove sold items
        config.KnownListings.RemoveAll(l => l.AssociatedRetainerId == activeRetainerId.Value && !fetchedListings.Any(f => f.ItemId == l.ItemId));

        this.configService.Save();
        this.logger.Info($"Successfully recorded {fetchedListings.Count} listings for retainer {activeRetainerId.Value}.");
    }

    public void Dispose() {
        this.gameEventService.RetainerListingsOpened -= this.RecordListings;
        this.gameEventService.RetainerListingAdded -= this.RecordListings;
    }
}