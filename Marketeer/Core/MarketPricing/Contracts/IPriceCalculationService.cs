using Marketeer.Core.MarketPricing.Models;

namespace Marketeer.Core.MarketPricing.Contracts;

public interface IPriceCalculationService {
    PriceCalculationResult CalculateTargetPrice(uint itemId, uint currentPrice, MarketItemPricing pricing);
}