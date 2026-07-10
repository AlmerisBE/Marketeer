using Marketeer.Features.CharacterTracking.Contracts;
using Marketeer.Features.CharacterTracking.Models;
using Marketeer.Features.Configuration.Contracts;
using Marketeer.Features.Logging.Contracts;
using Marketeer.Features.MarketListingTracking.Models;
using System.Collections.Generic;
using System.Linq;

namespace Marketeer.Features.MarketListingTracking.Services;

public class MarketListingTrackerService : IMarketListingTrackerService {
    private IConfigurationService configService;
    private ILoggerService logger;

    public MarketListingTrackerService(IConfigurationService configService, ILoggerService logger) {
        this.configService = configService;
        this.logger = logger;
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
}