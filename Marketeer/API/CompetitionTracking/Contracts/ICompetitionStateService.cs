using Marketeer.API.CompetitionTracking.Models;
using System.Collections.Generic;

namespace Marketeer.API.CompetitionTracking.Contracts;

public interface ICompetitionStateService {
    IReadOnlyList<UndercutItem> GetUndercutItems();
    void UpdateUndercuts(IEnumerable<UndercutItem> undercuts);
    void UpdateItemUndercuts(uint itemId, IEnumerable<UndercutItem> undercutsForItem);
}