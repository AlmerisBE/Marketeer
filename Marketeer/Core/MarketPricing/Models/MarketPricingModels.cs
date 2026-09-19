using Marketeer.API.Universalis.Models;
using System;
using System.Collections.Generic;

namespace Marketeer.Core.MarketPricing.Models;

public class CachedPriceData {
    public uint ItemId { get; set; }
    public IReadOnlyList<LowestPriceResult> Prices { get; set; } = new List<LowestPriceResult>();
    public DateTime LastUpdated { get; set; }
    public PriceSourceType Source { get; set; }
}