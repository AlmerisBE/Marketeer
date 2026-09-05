using Marketeer.Core.CompetitionTracking.Models;
using System.Collections.Generic;

namespace Marketeer.Core.CompetitionTracking.Contracts;

public interface IRetainerStateService {
    event System.Action<IEnumerable<RetainerListing>> ListingsUpdated;

    IReadOnlyList<RetainerListing> GetCurrentListings();
    IReadOnlyList<CharacterMarketData> GetAllCharactersListings();
    void UpdateListings(IEnumerable<RetainerListing> listings);

}