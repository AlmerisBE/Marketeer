namespace Marketeer.Core.CompetitionTracking.Contracts;

public interface ICompetitionPlayerContext {
    bool IsPlayerAvailable();
    uint GetCurrentWorldId();
}