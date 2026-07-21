using Marketeer.Features.Dashboard.Models;
using System;
using System.Collections.Generic;

namespace Marketeer.Features.Dashboard.Contracts;

public interface IMarketListingTrackerService {
    event Action<uint>? LocalListingModified;

    IReadOnlyList<ListingDisplayData> GetListingsForRetainer(ulong retainerId);
    bool ScanListings(ulong retainerId, bool isFirstScan);
}