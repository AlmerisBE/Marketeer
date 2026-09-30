using Marketeer.API.Universalis.Models;
using System;
using System.Collections.Generic;

namespace Marketeer.Core.MarketPricing.Models;

public class MarketItemPricing {
    public uint ItemId { get; set; }
    public IReadOnlyList<LowestPriceResult> Listings { get; set; } = new List<LowestPriceResult>();
    public uint AverageSalePrice { get; set; }
    public float SalesPerDay { get; set; }
}

public class CachedPriceData {
    public uint ItemId { get; set; }
    public MarketItemPricing Pricing { get; set; } = new();
    public DateTime LastUpdated { get; set; }
    public PriceSourceType Source { get; set; }
}