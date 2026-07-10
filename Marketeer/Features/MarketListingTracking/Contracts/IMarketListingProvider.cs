using Marketeer.Features.MarketListingTracking.Models;
using System.Collections.Generic;

namespace Marketeer.Features.MarketListingTracking.Contracts;

public interface IMarketListingProvider {
    ulong? GetActiveRetainerId();
    IReadOnlyList<TrackedListing> GetActiveRetainerListings();
}