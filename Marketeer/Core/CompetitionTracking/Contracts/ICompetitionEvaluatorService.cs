using Marketeer.Core.CompetitionTracking.Models;
using Marketeer.Core.MarketPricing.Models;
using Marketeer.UI.CompetitionTracking.Models;

namespace Marketeer.Core.CompetitionTracking.Contracts;

public interface ICompetitionEvaluatorService {
    bool TryEvaluateListing(RetainerListing listing, MarketItemPricing pricing, string characterName, string resolvedItemName, out UndercutItem? undercutResult);
}