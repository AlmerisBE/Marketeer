using Marketeer.Core.MarketStrategy.Models;

namespace Marketeer.Core.CompetitionTracking.Contracts;

public interface ICompetitionRuleProvider {
    PricingRule GetRuleForItem(uint itemId);
}