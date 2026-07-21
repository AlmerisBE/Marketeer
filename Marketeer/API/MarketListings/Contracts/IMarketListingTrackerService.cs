using Marketeer.API.MarketListings.Models;
using System;
using System.Collections.Generic;

namespace Marketeer.API.MarketListings.Contracts;

public interface IMarketListingTrackerService {
    event Action<uint>? LocalListingModified;

    IReadOnlyList<ListingDisplayData> GetListingsForRetainer(ulong retainerId);
    bool ScanListings(ulong retainerId, bool isFirstScan);
}