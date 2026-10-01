using Marketeer.Core.MarketPricing.Models;
using Marketeer.Core.MarketStrategy.Models;

namespace Marketeer.Core.MarketStrategy.Contracts;

public interface IMarketAnomalyDetector {
    AnomalyReport EvaluateMarket(MarketItemPricing pricing, bool isHq);
}