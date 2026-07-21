using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Marketeer.Features.Universalis.Models;

public class UniversalisMultiResponse {
    [JsonPropertyName("items")]
    public Dictionary<uint, UniversalisResponse> Items { get; set; } = new();
}