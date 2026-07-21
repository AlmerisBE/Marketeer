using Marketeer.Features.UndercutTracking.Models;
using System.Collections.Generic;

namespace Marketeer.Features.UndercutTracking.Contracts;

public interface IRetainerStateService {
    IReadOnlyList<RetainerListing> GetCurrentListings();
    void UpdateListings(IEnumerable<RetainerListing> newListings);
}