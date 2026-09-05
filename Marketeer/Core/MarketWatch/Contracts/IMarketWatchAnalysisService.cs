using Marketeer.Core.MarketWatch.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Marketeer.Core.MarketWatch.Contracts;

public interface IMarketWatchAnalysisService {
    Task<IReadOnlyList<MarketWatchAlert>> AnalyzeMarketAsync(bool bypassCache = false);
}