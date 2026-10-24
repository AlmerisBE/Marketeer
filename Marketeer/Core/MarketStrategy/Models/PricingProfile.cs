using System;

namespace Marketeer.Core.MarketStrategy.Models;

public class PricingProfile {
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public PricingRule Rule { get; set; } = new();
}