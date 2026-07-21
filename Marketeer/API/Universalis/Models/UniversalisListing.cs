using System.Text.Json.Serialization;

namespace Marketeer.API.Universalis.Models;

public class UniversalisListing {
    [JsonPropertyName("pricePerUnit")]
    public uint PricePerUnit { get; set; }

    [JsonPropertyName("retainerName")]
    public string RetainerName { get; set; } = string.Empty;
}