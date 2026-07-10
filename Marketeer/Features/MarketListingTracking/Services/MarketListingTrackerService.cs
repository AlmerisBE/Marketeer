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

        config.KnownListings.RemoveAll(l => l.AssociatedRetainerId == activeRetainerId.Value);
        config.KnownListings.AddRange(fetchedListings);

        this.configService.Save();
        this.logger.Info($"Successfully recorded {fetchedListings.Count} listings for retainer {activeRetainerId.Value}.");
    }

    public void Dispose() {
        this.gameEventService.RetainerListingsOpened -= this.RecordListings;
    }
}