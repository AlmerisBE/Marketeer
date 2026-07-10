using Marketeer.Features.Dashboard.Models;
using System.Collections.Generic;

namespace Marketeer.Features.Dashboard.Contracts;

public interface IMarketListingTrackerService {
    IReadOnlyList<ListingDisplayData> GetListingsForRetainer(ulong retainerId);
}