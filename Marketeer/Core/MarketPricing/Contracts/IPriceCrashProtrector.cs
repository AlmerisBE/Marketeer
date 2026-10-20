using Marketeer.API.Universalis.Models;
using Marketeer.Core.MarketPricing.Models;
using System.Collections.Generic;

namespace Marketeer.Core.MarketPricing.Contracts;

public interface IPriceCrashProtector {
    void ApplyProtection(MarketItemPricing pricing, bool isHq, List<LowestPriceResult> validCompetitors, out bool holdPrice);
}