using Marketeer.Features.UndercutTracking.Models;
using System.Collections.Generic;

namespace Marketeer.Features.UndercutTracking.Contracts;

public interface IRetainerStateService {
    event System.Action<IEnumerable<RetainerListing>> ListingsUpdated;

    IReadOnlyList<RetainerListing> GetCurrentListings();
    IReadOnlyList<CharacterMarketData> GetAllCharactersListings();
    void UpdateListings(IEnumerable<RetainerListing> listings);
}