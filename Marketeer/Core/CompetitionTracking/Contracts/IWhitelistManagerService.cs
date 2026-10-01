using System.Collections.Generic;

namespace Marketeer.Core.CompetitionTracking.Contracts;

public interface IWhitelistManagerService {
    void AutoAddKnownRetainers();
    void AddToWhitelist(string retainerName);
    void RemoveFromWhitelist(string retainerName);
    bool IsWhitelisted(string retainerName);
    IReadOnlyCollection<string> GetWhitelistedRetainers();
}