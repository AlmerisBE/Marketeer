using Marketeer.Features.UndercutTracking.Models;
using System.Collections.Generic;

namespace Marketeer.Features.UndercutTracking.Contracts;

public interface IRetainerStateService {
    event System.Action<IEnumerable<RetainerListing>> ListingsUpdated;

    IReadOnlyList<RetainerListing> GetCurrentListings();
    void UpdateListings(IEnumerable<RetainerListing> listings);
}