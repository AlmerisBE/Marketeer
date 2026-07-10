using Marketeer.Features.CharacterTracking.Models;
using System.Collections.Generic;

namespace Marketeer.Features.CharacterTracking.Contracts;

public interface IMarketListingTrackerService {
    IReadOnlyList<ListingDisplayData> GetListingsForRetainer(ulong retainerId);
}