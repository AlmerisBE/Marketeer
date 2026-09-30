using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Marketeer.API.Universalis.Models;

public class UniversalisResponse {
    [JsonPropertyName("itemID")]
    public uint ItemId { get; set; }

    [JsonPropertyName("listings")]
    public List<UniversalisListing> Listings { get; set; } = new();

    [JsonPropertyName("averagePriceNQ")]
    public float AveragePriceNq { get; set; }

    [JsonPropertyName("averagePriceHQ")]
    public float AveragePriceHq { get; set; }

    [JsonPropertyName("nqSaleVelocity")]
    public float NqSaleVelocity { get; set; }

    [JsonPropertyName("hqSaleVelocity")]
    public float HqSaleVelocity { get; set; }
}