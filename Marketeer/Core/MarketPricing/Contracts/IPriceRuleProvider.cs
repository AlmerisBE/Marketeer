using Marketeer.Core.Configuration.Models;

namespace Marketeer.Core.MarketPricing.Contracts;

public interface IPriceRuleProvider {
    PricingRule GetRuleForItem(uint itemId);
}