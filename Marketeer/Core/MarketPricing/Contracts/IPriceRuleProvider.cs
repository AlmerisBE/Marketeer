using Marketeer.Core.MarketStrategy.Models;

namespace Marketeer.Core.MarketPricing.Contracts;

public interface IPriceRuleProvider {
    PricingRule GetRuleForItem(uint itemId);
}