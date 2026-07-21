using Marketeer.API.MarketListings.Models;
using System.Collections.Generic;

namespace Marketeer.API.GameInterop.Contracts;

public interface IMarketListingProvider {
    ulong? GetActiveRetainerId();
    IReadOnlyList<TrackedListing> GetActiveRetainerListings();
}