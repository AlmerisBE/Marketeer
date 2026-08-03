using Marketeer.API.MarketWatch.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Marketeer.API.MarketWatch.Contracts;

public interface IMarketWatchAnalysisService {
    Task<IReadOnlyList<MarketWatchAlert>> AnalyzeMarketAsync();
}