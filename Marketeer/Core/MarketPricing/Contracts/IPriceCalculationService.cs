using Marketeer.API.Universalis.Models;
using Marketeer.Core.MarketPricing.Models;
using System.Collections.Generic;

namespace Marketeer.Core.MarketPricing.Contracts;

public interface IPriceCalculationService {
    PriceCalculationResult CalculateTargetPrice(uint itemId, uint currentPrice, IReadOnlyList<LowestPriceResult> activeListings);
}