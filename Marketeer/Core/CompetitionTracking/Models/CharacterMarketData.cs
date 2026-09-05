using System.Collections.Generic;

namespace Marketeer.Core.CompetitionTracking.Models;

public class CharacterMarketData {
    public string CharacterName { get; set; } = string.Empty;
    public uint HomeWorldId { get; set; }
    public List<RetainerListing> Listings { get; set; } = new();
}