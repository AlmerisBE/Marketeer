namespace Marketeer.Core.MarketWatch.Contracts;

public interface IMarketWatchPlayerContext {
    bool IsPlayerAvailable();
    uint GetCurrentWorldId();
}