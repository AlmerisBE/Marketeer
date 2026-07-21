using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Marketeer.Features.Universalis.Models;

public class UniversalisResponse {
    [JsonPropertyName("itemID")]
    public uint ItemId { get; set; }

    [JsonPropertyName("listings")]
    public List<UniversalisListing> Listings { get; set; } = new();
}