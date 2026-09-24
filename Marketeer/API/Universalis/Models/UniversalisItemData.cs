using System.Collections.Generic;

namespace Marketeer.API.Universalis.Models;

public class UniversalisItemData {
    public uint BaseItemId { get; set; }
    public List<LowestPriceResult> Listings { get; set; } = new();
    public uint AveragePriceNq { get; set; }
    public uint AveragePriceHq { get; set; }
}