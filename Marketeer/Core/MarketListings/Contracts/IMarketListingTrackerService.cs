using Marketeer.Core.MarketListings.Models;
using System;
using System.Collections.Generic;

namespace Marketeer.Core.MarketListings.Contracts;

public interface IMarketListingTrackerService {
    event Action<uint>? LocalListingModified;

    IReadOnlyList<ListingDisplayData> GetListingsForRetainer(ulong retainerId);
    bool ScanListings(ulong retainerId, bool isFirstScan);
    void RegisterPriceUpdate(ulong retainerId, uint itemId, uint newPrice);
}